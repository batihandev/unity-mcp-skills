"""The ten starter layouts share this single content owner."""
from pathlib import PurePosixPath
from xml.etree import ElementTree as ET
from .uitoolkit import NS, asset_path
STARTER_USS = ':root {\n    --space: 8px;\n    --surface: #253044;\n}\n.screen { padding-left: var(--space); padding-right: var(--space); padding-top: var(--space); padding-bottom: var(--space); flex-grow: 1; }\n.heading { font-size: 22px; -unity-font-style: bold; margin-bottom: var(--space); }\n.row { flex-direction: row; flex-wrap: wrap; }\n'
LAYOUTS = {'menu': [('Label', 'title', {'text': 'Main menu', 'class': 'heading'}, []),
          ('Button', 'play', {'text': 'Play'}, []),
          ('Button', 'options', {'text': 'Settings'}, [])],
 'hud': [('Label', 'score', {'text': 'Score: 0', 'class': 'heading'}, []),
         ('ProgressBar', 'health', {'title': 'Health', 'value': '100'}, []),
         ('Label', 'objective', {'text': 'Reach the exit'}, [])],
 'dialog': [('Label', 'title', {'text': 'Confirm action', 'class': 'heading'}, []),
            ('Label', 'message', {'text': 'Continue with this action?'}, []),
            ('VisualElement',
             'actions',
             {'class': 'row'},
             [('Button', 'cancel', {'text': 'Cancel'}, []),
              ('Button', 'confirm', {'text': 'Confirm'}, [])])],
 'settings': [('Label', 'title', {'text': 'Settings', 'class': 'heading'}, []),
              ('Slider',
               'volume',
               {'label': 'Volume', 'low-value': '0', 'high-value': '1', 'value': '0.8'},
               []),
              ('Toggle', 'sound', {'label': 'Sound enabled', 'value': 'true'}, [])],
 'inventory': [('Label', 'title', {'text': 'Inventory', 'class': 'heading'}, []),
               ('VisualElement',
                'items',
                {'class': 'row'},
                [('Button', 'item-one', {'text': 'Item 1'}, []),
                 ('Button', 'item-two', {'text': 'Item 2'}, []),
                 ('Button', 'item-three', {'text': 'Item 3'}, [])])],
 'list': [('Label', 'title', {'text': 'Items', 'class': 'heading'}, []),
          ('ScrollView',
           'items',
           {},
           [('Label', 'item-one', {'text': 'First item'}, []),
            ('Label', 'item-two', {'text': 'Second item'}, []),
            ('Label', 'item-three', {'text': 'Third item'}, [])])],
 'tab-view': [('VisualElement',
               'tabs',
               {'class': 'row'},
               [('Button', 'tab-one', {'text': 'Overview'}, []),
                ('Button', 'tab-two', {'text': 'Details'}, [])]),
              ('VisualElement',
               'content',
               {},
               [('Label', 'tab-content', {'text': 'Overview content'}, [])])],
 'toolbar': [('VisualElement',
              'tools',
              {'class': 'row'},
              [('Button', 'new', {'text': 'New'}, []),
               ('Button', 'save', {'text': 'Save'}, []),
               ('TextField', 'search', {'label': 'Search'}, [])])],
 'card': [('Label', 'title', {'text': 'Card title', 'class': 'heading'}, []),
          ('Label', 'description', {'text': 'A short description'}, []),
          ('Button', 'action', {'text': 'Open'}, [])],
 'notification': [('Label', 'title', {'text': 'Notification', 'class': 'heading'}, []),
                  ('Label', 'message', {'text': 'Your changes were saved'}, []),
                  ('Button', 'dismiss', {'text': 'Dismiss'}, [])]}

def render(request):
    name = request.get('template')
    if name not in LAYOUTS:
        raise ValueError('Unknown starter template')
    directory = request['savePath']
    if not isinstance(directory, str) or (directory != 'Assets' and not directory.startswith('Assets/')) or '\\' in directory or any((p in ('', '..', '.') for p in directory.split('/'))):
        raise ValueError('Expected a canonical Assets/ output directory')
    file_name = request.get('name') if request.get('name') is not None else name.title()
    if not isinstance(file_name, str) or not file_name or any((c in file_name for c in '/\\')) or ('..' in file_name) or any((ord(c) < 32 for c in file_name)):
        raise ValueError('Expected a single output file name')
    path = asset_path(directory + '/' + file_name + '.uxml', '.uxml')
    style = str(PurePosixPath(path).with_suffix('.uss'))
    ET.register_namespace('ui', NS)
    root = ET.Element('{' + NS + '}UXML')
    ET.SubElement(root, 'Style', {'src': PurePosixPath(style).name})
    container = ET.SubElement(root, '{' + NS + '}VisualElement', {'name': name + '-root', 'class': 'screen'})

    def append(parent, items):
        for typ, query, attrs, children in items:
            child = ET.SubElement(parent, '{' + NS + '}' + typ, {'name': query, **attrs})
            append(child, children)
    append(container, LAYOUTS[name])
    return {'template': name, 'name': file_name, 'uxmlPath': path, 'ussPath': style, 'writes': [{'path': style, 'text': STARTER_USS}, {'path': path, 'text': ET.tostring(root, encoding='unicode') + '\n'}], 'behavior': 'Starter layout only; wire application callbacks separately'}
