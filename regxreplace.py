import json
import os
import argparse
import re


class Rule:
    def __init__(self, pattern, replace):
        self.pattern = re.compile(pattern)
        self.replace = replace

    def convert(self, content):
        return self.pattern.sub(self.replace, content)


def apply_rules(rules: list, content: str):
    for rule in rules:
        content = rule.convert(content)
    return content


def process_file(dir_name: str, file_name: str, name_rules: list, content_rules: list):
    new_file_name = apply_rules(name_rules, file_name)
    item = os.path.join(dir_name, file_name)
    file_id = item
    if new_file_name != file_name:
        file_id = os.path.join(dir_name, new_file_name)
        os.renames(item, file_id)

    if os.path.isfile(file_id):
        if len(content_rules) > 0:
            with open(file_id) as tf:
                content = tf.read()
            content = apply_rules(content_rules, content)
            with open(file_id, "w") as tf:
                tf.write(content)
    return file_id


def process_dir(dir_name: str, recursive: bool, name_rules: list, content_rules: list):
    files = os.listdir(dir_name)
    for f in files:
        item = process_file(dir_name, f, name_rules, content_rules)
        if os.path.isdir(item) and recursive:
            process_dir(item, True, name_rules, content_rules)


def parse_rules(rule_list: list):
    if not rule_list:
        return []
    rule_set = []
    for rule in rule_list:
        rule_set.append(Rule(rule.get('reg'), rule.get('sub')))
    return rule_set


parser = argparse.ArgumentParser(description='Search path.')
parser.add_argument('-p', '--path',
                    help='Search path', default=os.getcwd())
parser.add_argument('-c', '--config',
                    help='Config file', default='rename.json')
parser.add_argument('-r', '--recursive',
                    help='Include sub directories', action='store_true', default=True)

args = parser.parse_args()
with open(args.config) as config:
    sections = json.load(config)
    content_rules = []
    name_rules = []
    rules = parse_rules(sections.get('common'))
    content_rules = content_rules + rules
    name_rules = name_rules + rules
    rules = parse_rules(sections.get('fileNames'))
    name_rules = name_rules + rules
    rules = parse_rules(sections.get('contents'))
    content_rules = content_rules + rules
    process_dir(args.path, args.recursive, name_rules, content_rules)
