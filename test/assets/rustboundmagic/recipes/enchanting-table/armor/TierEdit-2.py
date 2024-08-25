import re
import os
import json

TRAILING_INTEGER_RE = r'\d+$'

def increment_trailing(s):
    trailing_int = re.search(TRAILING_INTEGER_RE, s)
    if trailing_int:
        incremented_int = int(trailing_int.group()) + 1
        return re.sub(TRAILING_INTEGER_RE, '', s) + str(incremented_int)

def increment_dict(d):
    for key in d:
        d[key] += 1
        return d

def process_json_file(filepath):
    try:
        with open(filepath, 'r') as file:
            data = json.load(file)
            print(f"Contents of {filepath}:")
            for recipe in data:
                print(recipe)
                file_name = os.path.basename(filepath.rstrip('.json'))
                new_filename = increment_trailing(file_name) + '.json'
                new_name = increment_trailing(recipe['name'])
                new_enchantment = increment_dict(recipe['enchantments'])
                recipe['processingHours'] = int(recipe['processingHours']) * 4
                recipe['ingredients']['reagent']['quantity'] = int(recipe['ingredients']['reagent']['quantity'] ) * 4
                print(f'new_filename  {new_filename}')
                recipe['name'] = new_name
                print(f'new_name  {new_name}')
                recipe['enchantments'] = new_enchantment
                print(f'new_enchantment {new_enchantment}')
                print(recipe)
            new_file_path = os.path.join(os.path.dirname(filepath), new_filename)
            with open(new_file_path, 'w', encoding='utf-8') as newfile:
                json.dump(data, newfile, indent=4, ensure_ascii=False)

    except json.JSONDecodeError as e:
        print(f"Error decoding JSON in file {filepath}: {e}")
    except Exception as e:
        print(f"Error opening file {filepath}: {e}")

def walk_directory_tree(root_dir):
    for root, dirs, files in os.walk(root_dir):
        for file in files:
            if file.lower().endswith('-1.json'):
                filepath = os.path.join(root, file)
                process_json_file(filepath)

root_directory = '.'
walk_directory_tree(root_directory)