"""Pure UI Toolkit content transforms. Publication belongs to authoring.transact."""
import copy
import json
import io
import re
from pathlib import PurePosixPath
from xml.etree import ElementTree as ET
NS = 'UnityEngine.UIElements'
from . import authoring


def identifier(value):
    if not isinstance(value, str):
        raise ValueError('Expected a canonical C# identifier')
    try:
        authoring.identifier(value)
    except authoring.Refusal as error:
        raise ValueError(str(error)) from error
    return value


def literal(value):
    if not isinstance(value, str):
        raise ValueError('Expected a string literal')
    return json.dumps(value, ensure_ascii=True)

def parse_xml(source):
    if not isinstance(source, str) or re.search('<!\\s*(?:DOCTYPE|ENTITY)\\b', source, re.I):
        raise ValueError('Malformed or unsupported XML declarations')
    try:
        namespaces = list(ET.iterparse(io.StringIO(source), events=('start-ns',)))
        for _, (prefix, uri) in namespaces:
            # ElementTree reserves ns<number> prefixes for its serializer.
            if not re.fullmatch(r'ns[0-9]+', prefix):
                ET.register_namespace(prefix, uri)
        root = ET.fromstring(source, parser=ET.XMLParser(target=ET.TreeBuilder(insert_comments=True)))
    except (ET.ParseError, ValueError) as error:
        raise ValueError('Malformed XML') from error
    if root.tag not in ('UXML', '{' + NS + '}UXML'):
        raise ValueError('Expected a Unity UIElements UXML root')
    return root

def local(tag):
    return tag.rsplit('}', 1)[-1] if isinstance(tag, str) else '#comment'

def unique(root, name):
    if not isinstance(name, str) or not name:
        raise ValueError('Element name must be a nonempty string')
    found = [e for e in root.iter() if isinstance(e.tag, str) and e.get('name') == name]
    if not name or len(found) != 1:
        raise ValueError('Element name must resolve exactly once: ' + str(name))
    return found[0]

def attributes(request):
    supplied = request.get('attributes', {})
    if not isinstance(supplied, dict):
        raise ValueError('XML attributes must be an object')
    attrs = dict(supplied)
    for key, target in [('elementName', 'name'), ('text', 'text'), ('classes', 'class'), ('style', 'style'), ('bindingPath', 'binding-path'), ('newName', 'name')]:
        if key in request:
            attrs[target] = request[key]
    for key, value in attrs.items():
        if not isinstance(key, str) or not re.fullmatch('[A-Za-z_][A-Za-z0-9_.-]*', key) or (not isinstance(value, str)):
            raise ValueError('XML attributes require valid names and string values')
    return attrs

def xml_transform(request):
    root = parse_xml(request['source'])
    operation = request['operation']
    if operation == 'xml-inspect':
        depth = request.get('depth', 5)
        if type(depth) is not int or depth < 0:
            raise ValueError('Depth must be a nonnegative integer')

        def tree(element, level):
            return {'type': local(element.tag), 'namespace': element.tag[1:].split('}')[0] if isinstance(element.tag, str) and element.tag.startswith('{') else None, 'attributes': dict(element.attrib), 'children': [tree(c, level + 1) for c in element if isinstance(c.tag, str)] if level < depth else []}
        return {'tree': tree(root, 0), 'depth': depth}
    if operation == 'xml-add':
        parent_name = request.get('parentName')
        if parent_name is not None and not isinstance(parent_name, str):
            raise ValueError('Parent name must be a string')
        parent = unique(root, parent_name) if parent_name else root
        element_type = request.get('elementType')
        if not isinstance(element_type, str) or not re.fullmatch('[A-Za-z_][A-Za-z0-9_.]*', element_type):
            raise ValueError('Invalid UIElements type token')
        tag = '{' + NS + '}' + element_type if root.tag.startswith('{') else element_type
        parent.append(ET.Element(tag, attributes(request)))
    else:
        target = unique(root, request.get('elementName'))
        if operation == 'xml-modify':
            updates = attributes({k: v for k, v in request.items() if k != 'elementName'})
            target.attrib.update(updates)
        elif operation in ('xml-remove', 'xml-clone'):
            if target is root:
                raise ValueError('Cannot remove or clone the document root')
            parent = next((p for p in root.iter() if target in list(p)))
            if operation == 'xml-remove':
                parent.remove(target)
            else:
                new_name = request.get('newName')
                if not isinstance(new_name, str) or not new_name or new_name == target.get('name') or any((e.get('name') == new_name for e in root.iter())):
                    raise ValueError('Clone requires a distinct unique newName')
                clone = copy.deepcopy(target)
                clone.set('name', new_name)
                parent.insert(list(parent).index(target) + 1, clone)
        else:
            raise ValueError('Unknown XML operation')
    return {'content': ET.tostring(root, encoding='unicode') + '\n', 'normalization': 'XML serialization normalizes formatting and namespace prefixes and escapes attributes'}

def lexical_mask(source, mask_literals=True):
    """Replace comments/quoted strings with spaces while retaining source offsets."""
    if not isinstance(source, str):
        raise ValueError('USS source must be a string')
    output = list(source)
    i = 0
    while i < len(source):
        if source.startswith('/*', i):
            end = source.find('*/', i + 2)
            if end < 0:
                raise ValueError('Unterminated USS comment')
            end += 2
            output[i:end] = ' ' * (end - i)
            i = end
        elif source[i] in '"\'':
            quote = source[i]
            start = i
            i += 1
            while i < len(source):
                if source[i] == '\\':
                    i += 2
                    continue
                if source[i] == quote:
                    i += 1
                    break
                i += 1
            else:
                raise ValueError('Unterminated USS string')
            if mask_literals:
                output[start:i] = ' ' * (i - start)
        elif source[i] == '\\':
            if i + 1 >= len(source):
                raise ValueError('Dangling USS escape')
            if mask_literals:
                output[i:i + 2] = '  '
            i += 2
        else:
            i += 1
    return ''.join(output)

def rules(source):
    mask = lexical_mask(source)
    selector_source = lexical_mask(source, mask_literals=False)
    result = []
    start = 0
    opened = None
    delimiters = []
    for i, char in enumerate(mask):
        if char in '([':
            delimiters.append(char)
        elif char in ')]':
            if not delimiters or delimiters.pop() != ('(' if char == ')' else '['):
                raise ValueError('Unbalanced USS delimiters')
        if char == '{' and not delimiters:
            if opened is not None:
                raise ValueError('Nested USS rules are unsupported')
            head = selector_source[start:i].strip()
            if not head or head.startswith('@'):
                raise ValueError('Unsupported USS rule shape')
            offset = start + len(selector_source[start:i]) - len(selector_source[start:i].lstrip())
            opened = (offset, i, head)
        elif char == '}' and not delimiters:
            if opened is None:
                raise ValueError('Unbalanced USS braces')
            offset, body, selector = opened
            result.append({'start': offset, 'end': i + 1, 'bodyStart': body + 1, 'bodyEnd': i, 'selector': selector})
            start = i + 1
            opened = None
    if opened is not None or delimiters or mask[start:].strip():
        raise ValueError('Malformed USS structure')
    selectors = [r['selector'] for r in result]
    if len(selectors) != len(set(selectors)):
        raise ValueError('Ambiguous duplicate USS selector')
    return (result, mask)

def uss_transform(request):
    source = request['source']
    parsed, mask = rules(source)
    operation = request['operation']
    if operation == 'uss-variables':
        declarations = []
        for rule in parsed:
            start = rule['bodyStart']
            parens = 0
            brackets = 0
            for end in range(start, rule['bodyEnd'] + 1):
                char = mask[end] if end < rule['bodyEnd'] else ';'
                if char == '(':
                    parens += 1
                elif char == ')':
                    parens -= 1
                elif char == '[':
                    brackets += 1
                elif char == ']':
                    brackets -= 1
                if char == ';' and parens == 0 and brackets == 0:
                    match = re.match('\\s*(--[A-Za-z_][A-Za-z0-9_-]*)\\s*:', mask[start:end])
                    if match:
                        begin = start + match.end()
                        declarations.append({'name': match.group(1), 'value': source[begin:end].strip(), 'selector': rule['selector']})
                    start = end + 1
        refs = sorted(set(re.findall('\\bvar\\s*\\(\\s*(--[A-Za-z_][A-Za-z0-9_-]*)', mask)))
        return {'declarations': declarations, 'references': refs}
    selector = request.get('selector')
    if not isinstance(selector, str) or not selector.strip() or selector != selector.strip() or any((c in lexical_mask(selector) for c in '{};')) or selector.startswith('@'):
        raise ValueError('Expected an exact USS selector')
    matches = [r for r in parsed if r['selector'] == selector]
    if operation == 'uss-remove':
        if not matches:
            raise ValueError('Selector not found')
        rule = matches[0]
        return {'content': source[:rule['start']] + source[rule['end']:]}
    if operation != 'uss-upsert':
        raise ValueError('Unknown USS operation')
    props = request.get('properties')
    if isinstance(props, str):
        body = props.strip()
    elif isinstance(props, dict) and props:
        lines = []
        for key, value in props.items():
            if not isinstance(key, str) or not re.fullmatch('(?:--)?[A-Za-z_][A-Za-z0-9_-]*', key) or (not isinstance(value, str)):
                raise ValueError('Invalid USS declaration')
            lines.append('    ' + key + ': ' + value + ';')
        body = '\n'.join(lines)
    else:
        raise ValueError('Properties must be nonempty declarations')
    replacement = selector + ' {\n' + body + '\n}'
    checked, _ = rules(replacement)
    if len(checked) != 1 or checked[0]['selector'] != selector:
        raise ValueError('Properties must contain one declaration body')
    if matches:
        rule = matches[0]
        content = source[:rule['start']] + replacement + source[rule['end']:]
    else:
        content = source + ('' if not source or source.endswith('\n') else '\n') + replacement + '\n'
    return {'content': content}

def asset_path(value, suffix):
    if not isinstance(value, str) or '\\' in value or (not value.startswith('Assets/')) or any((p in ('', '..', '.') for p in value.split('/'))) or (not value.endswith(suffix)):
        raise ValueError('Expected a canonical Assets/ path ending in ' + suffix)
    return value

def scaffold(request):
    name = identifier(request['className'])
    path = asset_path(request['savePath'], '.cs')
    if PurePosixPath(path).stem != name:
        raise ValueError('Class and file names must match')
    if request['operation'] == 'editor-window':
        title = request.get('windowTitle') if request.get('windowTitle') is not None else name
        menu = request.get('menuPath') if request.get('menuPath') is not None else 'Window/' + name
        lines = ['using UnityEditor;', 'using UnityEngine;', 'using UnityEngine.UIElements;', '', f'public class {name} : EditorWindow', '{', f'    [MenuItem({literal(menu)})]', '    public static void ShowWindow()', '    {', f'        var window = GetWindow<{name}>();', f'        window.titleContent = new GUIContent({literal(title)});', '        window.minSize = new Vector2(400, 300);', '    }', '', '    public void CreateGUI()', '    {', '        var root = rootVisualElement;']
        if request.get('uxmlPath'):
            p = asset_path(request['uxmlPath'], '.uxml')
            lines += [f'        var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>({literal(p)});', '        if (tree == null) throw new System.InvalidOperationException("UXML asset unavailable");', '        tree.CloneTree(root);']
        else:
            lines += [f'        root.Add(new Label({literal(title)}));', '        root.Add(new Button { name = "action", text = "Action" });']
        if request.get('ussPath'):
            p = asset_path(request['ussPath'], '.uss')
            lines += [f'        var styles = AssetDatabase.LoadAssetAtPath<StyleSheet>({literal(p)});', '        if (styles == null) throw new System.InvalidOperationException("USS asset unavailable");', '        root.styleSheets.Add(styles);']
        lines += ['    }', '}']
        return {'content': '\n'.join(lines) + '\n', 'path': path, 'className': name, 'windowTitle': title, 'menuPath': menu, 'requiredAssets': [request[k] for k in ('uxmlPath', 'ussPath') if request.get(k)]}
    query = request.get('elementQueries')
    if query is not None and not isinstance(query, str):
        raise ValueError('Element queries must be a string')
    query = query or ''
    fields = []
    assignments = []
    used = set()
    for pair in query.split(',') if query else []:
        parts = pair.strip().split(':')
        if len(parts) != 2:
            raise ValueError('Queries require Type:name pairs')
        typ = parts[0].strip()
        element = parts[1].strip()
        for token in typ.split('.'):
            identifier(token)
        if not element:
            raise ValueError('Query name cannot be empty')
        field = 'm_' + element.replace('-', '').replace('_', '')
        identifier(field)
        if field in used:
            raise ValueError('Generated query fields collide')
        used.add(field)
        fields.append(f'    private {typ} {field};')
        assignments.append(f'        {field} = root.Q<{typ}>({literal(element)});')
    lines = ['using UnityEngine;', 'using UnityEngine.UIElements;', '', '[RequireComponent(typeof(UIDocument))]', f'public class {name} : MonoBehaviour', '{', *fields, '', '    private void OnEnable()', '    {', '        var root = GetComponent<UIDocument>().rootVisualElement;', '        if (root == null) return;', *assignments, '    }', '}']
    return {'content': '\n'.join(lines) + '\n', 'path': path, 'className': name}

def transform(request):
    operation = request.get('operation', '')
    if not isinstance(operation, str):
        raise ValueError('Operation must be a string')
    if operation.startswith('xml-'):
        return xml_transform(request)
    if operation.startswith('uss-'):
        return uss_transform(request)
    if operation in ('editor-window', 'runtime-ui'):
        return scaffold(request)
    if operation == 'template':
        from .uitoolkit_templates import render
        return render(request)
    if operation == 'create-uxml':
        path = asset_path(request['savePath'], '.uxml')
        content = request.get('content')
        if content is not None:
            parse_xml(content)
        else:
            ET.register_namespace('ui', NS)
            root = ET.Element('{' + NS + '}UXML')
            if request.get('ussPath'):
                style = asset_path(request['ussPath'], '.uss')
                reference = PurePosixPath(style).name if PurePosixPath(style).parent == PurePosixPath(path).parent else style
                ET.SubElement(root, 'Style', {'src': reference})
            name = request.get('name', 'root')
            if not isinstance(name, str):
                raise ValueError('Element name must be a string')
            ET.SubElement(root, '{' + NS + '}VisualElement', {'name': name})
            content = ET.tostring(root, encoding='unicode') + '\n'
        return {'content': content, 'path': path, 'requiredAssets': [request['ussPath']] if request.get('ussPath') else []}
    if operation == 'create-uss':
        from .uitoolkit_templates import STARTER_USS
        content = request.get('content') if request.get('content') is not None else STARTER_USS
        rules(content)
        return {'content': content}
    raise ValueError('Unknown content operation: ' + operation)
