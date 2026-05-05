
from pathlib import Path
import json
import re
import hashlib


BASE = Path(__file__).resolve().parent
SOURCE = BASE / 'source'
ABILITY_CODES = ['STR', 'DEX', 'CON', 'INT', 'WIS', 'CHA']
ABILITY_NAMES = {
    'strength': 'STR', 'dexterity': 'DEX', 'constitution': 'CON',
    'intelligence': 'INT', 'wisdom': 'WIS', 'charisma': 'CHA'
}
LANGUAGES = [
    'Common', 'Dwarvish', 'Elvish', 'Giant', 'Gnomish', 'Goblin', 'Halfling', 'Orc',
    'Abyssal', 'Celestial', 'Draconic', 'Deep Speech', 'Infernal', 'Primordial',
    'Sylvan', 'Undercommon', 'Aquan', 'Auran', 'Ignan', 'Terran', 'Leonin', 'Loxodon',
    'Minotaur', 'Vedalken'
]
SKILLS = ['Acrobatics', 'Animal Handling', 'Arcana', 'Athletics', 'Deception', 'History', 'Insight', 'Intimidation', 'Investigation', 'Medicine', 'Nature', 'Perception', 'Performance', 'Persuasion', 'Religion', 'Sleight of Hand', 'Stealth', 'Survival']
TOOLS = ["Alchemist's supplies", "Brewer's supplies", "Calligrapher's supplies", "Carpenter's tools", "Cartographer's tools", "Cobbler's tools", "Cook's utensils", "Glassblower's tools", "Jeweler's tools", "Leatherworker's tools", "Mason's tools", "Painter's supplies", "Potter's tools", "Smith's tools", "Tinker's tools", "Weaver's tools", "Woodcarver's tools", "Thieves' tools", 'Disguise kit', 'Forgery kit', 'Herbalism kit', 'Navigator\'s tools', 'Poisoner\'s kit']
WEAPONS = ['Battleaxe', 'Handaxe', 'Light hammer', 'Warhammer', 'Longsword', 'Shortsword', 'Shortbow', 'Longbow', 'Rapier', 'Hand crossbow', 'Spear', 'Trident', 'Light crossbow', 'Net']
SPELLCASTING_ABILITIES = ['INT', 'WIS', 'CHA']

NAME_OVERRIDES = {
    'harengons': 'Harengon',
    'hobgoblins': 'Hobgoblin',
    'yuan-ti': 'Yuan-ti Pureblood',
}

# parentId for races defined by their own source file (slug = slug(path.stem))
FILE_PARENT_BY_SLUG = {
    'duergar': 'race.dwarf',
    'astral_elf': 'race.elf',
    'eladrin': 'race.elf',
    'sea_elf': 'race.elf',
    'shadar_kai': 'race.elf',
    'air_genasi': 'race.genasi',
    'earth_genasi': 'race.genasi',
    'fire_genasi': 'race.genasi',
    'water_genasi': 'race.genasi',
    'githyanki': 'race.gith',
    'githzerai': 'race.gith',
    'deep_gnome': 'race.gnome',
    'bugbear': 'race.goblinoid',
    'goblin': 'race.goblinoid',
    'hobgoblins': 'race.goblinoid',
    'feral_tiefling': 'race.tiefling',
    'yuan_ti': 'race.yuan_ti',
}

# Do not emit a standalone playable race from these files (folder grouping / lore parents handled separately).
SKIP_SOURCE_STEMS = {'grung', 'dwarf', 'elf'}

TOP_LEVEL_ORDER = [
    'aarakocra',
    'aasimar',
    'autognome',
    'centaur',
    'changeling',
    'dhampir',
    'dragonborn',
    'dwarf',
    'elf',
    'fairy',
    'firbolg',
    'genasi',
    'geppettin',
    'giff',
    'gith',
    'gnome',
    'goblinoid',
    'goliath',
    'hadozee',
    'harengon',
    'hexblood',
    'kalashtar',
    'kender',
    'kenku',
    'kobold',
    'leonin',
    'lizardfolk',
    'minotaur',
    'orc',
    'owlin',
    'plasmoid',
    'reborn',
    'satyr',
    'shifter',
    'tabaxi',
    'thri_kreen',
    'tiefling',
    'tortle',
    'triton',
    'warforged',
    'yuan_ti',
]

CHILD_ORDER_BY_PARENT = {
    'race.dwarf': [
        'duergar',
    ],
    'race.elf': [
        'astral_elf',
        'eladrin',
        'sea_elf',
        'shadar_kai',
    ],
    'race.genasi': [
        'air_genasi',
        'earth_genasi',
        'fire_genasi',
        'water_genasi',
    ],
    'race.gith': [
        'githyanki',
        'githzerai',
    ],
    'race.gnome': [
        'deep_gnome',
    ],
    'race.goblinoid': [
        'bugbear',
        'goblin',
        'hobgoblin',
    ],
    'race.tiefling': [
        'feral_tiefling',
    ],
    'race.yuan_ti': [
        'yuan_ti_pureblood',
    ],
}


def clean(text):
    text = text.replace('\ufeff', '').replace('\u00ad', '')
    text = text.replace('\u2019', "'").replace('\u201c', '"').replace('\u201d', '"').replace('\u2014', '-')
    return re.sub(r'[ \t]+', ' ', text).strip()


def normalize_source_bytes(text):
    if text.startswith('\ufeff'):
        text = text[1:]
    text = text.replace('\r\n', '\n').replace('\r', '\n')
    text = text.replace('\u00ad', '')
    text = text.replace('\u2019', "'").replace('\u201c', '"').replace('\u201d', '"').replace('\u2014', '-')
    lines = [ln.rstrip() for ln in text.split('\n')]
    return '\n'.join(lines).rstrip() + '\n'


def normalize_source_files():
    for path in sorted(SOURCE.glob('*.txt')):
        raw = path.read_text(encoding='utf-8', errors='replace')
        norm = normalize_source_bytes(raw)
        if norm != raw:
            path.write_text(norm, encoding='utf-8')


def slug(name):
    name = re.sub(r'\(.*?\)', '', name.lower().replace('&', ' and '))
    return re.sub(r'[^a-z0-9]+', '_', name).strip('_')


def title_from_file(path):
    stem = path.stem.lower()
    return NAME_OVERRIDES.get(stem, path.stem.replace('-', ' ').title())


def first_paragraph(text):
    for para in re.split(r'\n\s*\n', text):
        para = clean(para)
        if len(para) > 30 and not para.startswith('-') and not re.match(r'^[A-Z][A-Za-z ]+ Traits$', para):
            return para[:1200]
    return ''


def first_lore_paragraph(text):
    """First substantive lore block for group headers (skips D&D Beyond-style titles and pull quotes)."""
    for para in re.split(r'\n\s*\n', text):
        para = clean(para)
        if len(para) < 100:
            continue
        if 'Species Details' in para or 'Legacy Content' in para:
            continue
        if para.startswith('"'):
            continue
        if para.startswith('- ') and ',' in para[:80]:
            continue
        if re.match(r'^[A-Z][A-Za-z ]+ Traits$', para):
            continue
        return para[:1200]
    return first_paragraph(text)


def is_heading(line):
    line = clean(line)
    if not line or len(line) > 80 or line.endswith('.') or '\t' in line:
        return False
    if re.search(r'\d', line):
        return False
    words = line.split()
    if len(words) > 7:
        return False
    small = {'and', 'of', 'the', 'or', 'in', 'with', 'to', 'a', 'an'}
    return all(w[:1].isupper() or w.isupper() or w.lower() in small for w in words)


def find_traits_start(lines, race_name):
    for idx, line in enumerate(lines):
        l = clean(line).lower()
        if l.endswith('traits') and (slug(race_name).replace('_', ' ') in l or l in {'race traits', 'traits'}):
            return idx + 1
    for idx, line in enumerate(lines):
        if re.match(r'^(Ability Score Increase|Ability Score Increases)\.', clean(line)) or clean(line) in {'Ability Score Increase', 'Ability Score Increases'}:
            return idx
    return 0


def parse_features(text, race_name):
    lines = [clean(l) for l in text.splitlines()]
    lines = [l for l in lines if l]
    start = find_traits_start(lines, race_name)
    lines = lines[start:]
    features = []
    i = 0
    inline_re = re.compile(r'^([A-Z][A-Za-z\' -]{2,55})\.\s+(.+)$')
    while i < len(lines):
        line = lines[i]
        inline = inline_re.match(line)
        if inline:
            name, body = inline.group(1).strip(), inline.group(2).strip()
            i += 1
            while i < len(lines) and not inline_re.match(lines[i]) and not is_heading(lines[i]):
                body += '\n\n' + lines[i]
                i += 1
            features.append({'name': name, 'description': body})
            continue
        if is_heading(line):
            name = line
            body_parts = []
            i += 1
            while i < len(lines) and not is_heading(lines[i]) and not inline_re.match(lines[i]):
                body_parts.append(lines[i])
                i += 1
            body = '\n\n'.join(body_parts).strip()
            if body:
                features.append({'name': name, 'description': body})
            continue
        i += 1
    skip = {'creating your character', 'languages', 'height and weight', 'creature type', 'life span'}
    out = []
    seen = set()
    for feat in features:
        key = feat['name'].lower()
        if key in skip or key in seen:
            continue
        seen.add(key)
        out.append(feat)
    return out


def merge_features(name, inherited, own):
    seen = set()
    merged = []
    for feat in (inherited or []) + (own or []):
        key = feat['name'].lower()
        if key in seen:
            continue
        seen.add(key)
        new_feat = dict(feat)
        new_feat['id'] = 'feature.' + slug(name) + '.' + slug(new_feat['name'])
        merged.append(new_feat)
    return merged


def parse_asi(text):
    fixed, choices = [], []
    lower = text.lower()
    if 'your ability scores each increase by 1' in lower:
        fixed.extend({'ability': a, 'bonus': 1} for a in ABILITY_CODES)
    for name, code in ABILITY_NAMES.items():
        for m in re.finditer(rf'Your {name} score increases by (\d)', text, re.I):
            fixed.append({'ability': code, 'bonus': int(m.group(1))})
    if re.search(r'increase one ability score by 2,? and increase a different (?:one|score) by 1|increase one score by 2 and increase a different score by 1', text, re.I):
        choices.append({'id': 'asi.choice_plus_2', 'name': 'Choose one ability', 'bonus': 2, 'requiresUniqueAbility': True, 'abilities': ABILITY_CODES})
        choices.append({'id': 'asi.choice_plus_1', 'name': 'Choose a different ability', 'bonus': 1, 'requiresUniqueAbility': True, 'abilities': ABILITY_CODES})
    if re.search(r'one other ability score of your choice increases by 1|another ability score of your choice increases by 1', text, re.I):
        choices.append({'id': 'asi.choice_plus_1', 'name': 'Choose one other ability', 'bonus': 1, 'requiresUniqueAbility': True, 'abilities': ABILITY_CODES})
    seen = set()
    deduped = []
    for item in fixed:
        key = (item['ability'], item['bonus'])
        if key not in seen:
            seen.add(key)
            deduped.append(item)
    return deduped, choices


def parse_size(text):
    if re.search(r'Medium or Small|Small or Medium', text, re.I):
        return 'Medium or Small'
    if re.search(r'You are Small|your size is Small', text, re.I):
        return 'Small'
    return 'Medium'


def parse_speed(text):
    m = re.search(r'walking speed increases to (\d+) feet', text, re.I)
    if m:
        return int(m.group(1))
    m = re.search(r'(?:base )?walking speed is (\d+) feet|speed is (\d+) feet', text, re.I)
    return int(next(g for g in m.groups() if g)) if m else 30


def parse_darkvision(text):
    if 'darkvision' not in text.lower():
        return 0
    m = re.search(r'(?:within|radius of|range of) (\d+) feet', text, re.I)
    return int(m.group(1)) if m else 60


def add_effect(effects, typ, name, target, value='', amount=0):
    effect = {'type': typ, 'name': name, 'target': target, 'value': value, 'amount': amount}
    key = (typ, target, value, amount)
    if key not in {(e['type'], e['target'], e['value'], e['amount']) for e in effects}:
        effects.append(effect)


def parse_languages(text, choices, effects):
    lang_line = None
    for m in re.finditer(r'Languages?\.\s+([^\n]+)|You can speak, read, and write ([^\.\n]+)\.', text, re.I):
        lang_line = clean(m.group(1) or m.group(2))
    if not lang_line:
        return
    normalized = lang_line.replace('Elven', 'Elvish')
    fixed = []
    for lang in LANGUAGES:
        if re.search(rf'\b{re.escape(lang)}\b', normalized, re.I) and 'choice' not in normalized[:normalized.lower().find(lang.lower()) if lang.lower() in normalized.lower() else len(normalized)].lower():
            fixed.append(lang)
    for lang in fixed:
        add_effect(effects, 'language', 'Language', 'language', lang)
    if re.search(r'one (?:other|extra) language|language of your choice|DM agree is appropriate', normalized, re.I):
        choices.append({'id': 'choice.language.extra', 'type': 'language', 'name': 'Choose an extra language', 'count': 1, 'options': [l for l in LANGUAGES if l not in fixed]})
    either = re.search(r'choice of either ([A-Za-z]+) or ([A-Za-z]+)', normalized, re.I)
    if either:
        opts = [either.group(1).replace('Elven', 'Elvish'), either.group(2).replace('Elven', 'Elvish')]
        choices.append({'id': 'choice.language.either', 'type': 'language', 'name': 'Choose a language', 'count': 1, 'options': opts})


def parse_choices(text, choices):
    cantrip = re.search(r'one of the following cantrips of your choice: ([^\.]+)', text, re.I)
    if cantrip:
        opts = [clean(x).title() for x in re.split(r',| or ', cantrip.group(1)) if clean(x)]
        choices.append({'id': 'choice.cantrip', 'type': 'cantrip', 'name': 'Choose a cantrip', 'count': 1, 'options': opts})
    skill = re.search(r'proficiency in one of the following skills of your choice: ([^\.]+)', text, re.I)
    if skill:
        opts = [clean(x) for x in re.split(r',| or ', skill.group(1)) if clean(x)]
        choices.append({'id': 'choice.skill', 'type': 'skill', 'name': 'Choose a skill proficiency', 'count': 1, 'options': opts})
    if re.search(r'one skill of your choice', text, re.I):
        choices.append({'id': 'choice.skill.any', 'type': 'skill', 'name': 'Choose a skill proficiency', 'count': 1, 'options': SKILLS})
    if re.search(r'two tool proficiencies of your choice|one weapon or tool of your choice|artisan.?s tools of your choice|tools of your choice', text, re.I):
        opts = TOOLS if 'weapon or tool' not in text.lower() else WEAPONS + TOOLS
        choices.append({'id': 'choice.tool', 'type': 'tool', 'name': 'Choose a tool or weapon proficiency', 'count': 1, 'options': opts})
    if re.search(r'Intelligence, Wisdom, or Charisma is your spellcasting ability.*choose', text, re.I | re.S):
        choices.append({'id': 'choice.spellcasting_ability', 'type': 'spellcastingAbility', 'name': 'Choose spellcasting ability', 'count': 1, 'options': SPELLCASTING_ABILITIES})


def parse_effects(text, features):
    effects = []
    lower = text.lower()
    for mode, pattern in [('fly', r'flying speed (?:equal to your walking speed|of (\d+) feet)'), ('swim', r'swimming speed of (\d+) feet'), ('climb', r'climbing speed of (\d+) feet')]:
        m = re.search(pattern, text, re.I)
        if m:
            amount = int(m.group(1)) if m.groups() and m.group(1) else 0
            add_effect(effects, 'speed', mode.title() + ' Speed', mode, f'{amount} ft' if amount else 'equal to walking speed', amount)
    ac = re.search(r'(?:base Armor Class is|base AC of|AC is) (\d+)', text, re.I)
    if ac:
        add_effect(effects, 'naturalArmor', 'Natural Armor', 'armorClass', f'Base AC {ac.group(1)}', int(ac.group(1)))
    for damage in ['poison', 'fire', 'cold', 'acid', 'lightning', 'necrotic', 'radiant', 'psychic']:
        if re.search(rf'resistance (?:against|to) {damage} damage|{damage} damage resistance', lower):
            add_effect(effects, 'resistance', damage.title() + ' Resistance', damage, damage.title() + ' damage')
    if re.search(r'proficiency in the Perception skill', text, re.I):
        add_effect(effects, 'proficiency', 'Keen Senses', 'skill', 'Perception')
    for feat in features:
        body = feat.get('description', '')
        if 'proficiency with' in body.lower() and 'choice' not in body.lower():
            value = re.sub(r'^You (?:have|gain) proficiency with ', '', body, flags=re.I).split('.')[0]
            target = 'tool' if 'tool' in value.lower() or 'supplies' in value.lower() else 'weapon'
            add_effect(effects, 'proficiency', feat['name'], target, clean(value))
    ctype = re.search(r'Creature Type\.\s+You are a ([A-Za-z]+)', text)
    if ctype:
        add_effect(effects, 'creatureType', 'Creature Type', 'creatureType', ctype.group(1))
    return effects


def build_race(name, source_file, text, parent_id='', inherited_text='', inherited_features=None, group_only=False):
    own_features = parse_features(text, name)
    features = merge_features(name, inherited_features, own_features)
    merged_text = (inherited_text + '\n' + text).strip()
    fixed, asi_choices = parse_asi(merged_text)
    choices = []
    effects = parse_effects(merged_text, features)
    parse_languages(merged_text, choices, effects)
    parse_choices(merged_text, choices)
    darkvision = parse_darkvision(merged_text)
    rid = 'race.' + slug(name)
    return {
        'id': rid,
        'name': name,
        'parentId': parent_id,
        'sortName': name,
        'sourceFile': source_file,
        'isGroupOnly': group_only,
        'description': first_paragraph(text),
        'abilityScoreBonuses': fixed,
        'abilityScoreChoices': asi_choices,
        'selectableChoices': choices,
        'mechanicalEffects': effects,
        'speed': parse_speed(merged_text),
        'size': parse_size(merged_text),
        'hasDarkvision': darkvision > 0,
        'darkvisionRange': darkvision,
        'traits': [f['name'] for f in features],
        'features': features,
    }


def synthetic_group(rid, name, description):
    return {
        'id': rid,
        'name': name,
        'parentId': '',
        'sortName': name,
        'sourceFile': '',
        'isGroupOnly': True,
        'description': description,
        'abilityScoreBonuses': [],
        'abilityScoreChoices': [],
        'selectableChoices': [],
        'mechanicalEffects': [],
        'speed': 30,
        'size': 'Medium',
        'hasDarkvision': False,
        'darkvisionRange': 0,
        'traits': [],
        'features': [],
    }


def build_sort_name_map():
    sort_names = {}
    step = 10
    for slug_tail in TOP_LEVEL_ORDER:
        rid = f'race.{slug_tail}'
        sort_names[rid] = f'{step:04d}'
        if rid in CHILD_ORDER_BY_PARENT:
            for j, child_tail in enumerate(CHILD_ORDER_BY_PARENT[rid], 1):
                sort_names[f'race.{child_tail}'] = f'{step:04d}{j:02d}'
        step += 10
    return sort_names


def apply_sort_names(races):
    smap = build_sort_name_map()
    for r in races:
        sid = r['id']
        r['sortName'] = smap.get(sid, r.get('sortName') or r.get('name') or sid)


def mark_parents_group_only(races):
    parents = {r['parentId'] for r in races if r.get('parentId')}
    for r in races:
        if r['id'] in parents:
            r['isGroupOnly'] = True


def write_meta(json_path):
    meta = json_path.with_suffix(json_path.suffix + '.meta')
    if meta.exists():
        return
    guid = hashlib.md5(str(json_path).replace('\\', '/').encode()).hexdigest()
    meta.write_text(
        f'fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n'
        f'  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n',
        encoding='utf-8',
    )


def lore_from_skipped_source(stem_lower):
    path = None
    for p in SOURCE.glob('*.txt'):
        if p.stem.lower() == stem_lower:
            path = p
            break
    if not path:
        return ''
    raw = path.read_text(encoding='utf-8', errors='replace')
    return first_lore_paragraph(raw)


def synthetic_parents():
    dwarf_desc = lore_from_skipped_source('dwarf') or 'Bold and hardy folk known for skill in stone and metal.'
    elf_desc = lore_from_skipped_source('elf') or 'Magical people of otherworldly grace and keen senses.'
    return [
        synthetic_group('race.dwarf', 'Dwarf', dwarf_desc),
        synthetic_group('race.elf', 'Elf', elf_desc),
        synthetic_group('race.genasi', 'Genasi', 'Mortals infused with elemental power.'),
        synthetic_group('race.gith', 'Gith', 'Astral wanderers and disciplined monks split between conquerors and ascetics.'),
        synthetic_group('race.gnome', 'Gnome', 'Clever tinkers and illusionists with boundless curiosity.'),
        synthetic_group('race.goblinoid', 'Goblinoid', 'Related goblin kin sharing roots in goblinoid ancestry.'),
        synthetic_group('race.tiefling', 'Tiefling', 'Mortals touched by infernal or planar heritage.'),
        synthetic_group('race.yuan_ti', 'Yuan-ti', 'Serpent folk blending human and ophidian traits.'),
    ]


def main():
    normalize_source_files()

    for p in list(BASE.glob('*.json')) + list(BASE.glob('*.json.meta')):
        p.unlink()

    races = []
    ambiguity = []

    races.extend(synthetic_parents())

    for path in sorted(SOURCE.glob('*.txt'), key=lambda p: p.name.lower()):
        stem_l = path.stem.lower()
        if stem_l in SKIP_SOURCE_STEMS:
            continue

        raw = path.read_text(encoding='utf-8', errors='replace').replace('\r\n', '\n').replace('\r', '\n')
        file_slug = slug(path.stem)
        name = title_from_file(path)
        parent_id = FILE_PARENT_BY_SLUG.get(file_slug, '')

        race = build_race(name, path.name, raw, parent_id, group_only=False)
        races.append(race)

    mark_parents_group_only(races)
    apply_sort_names(races)

    seen = set()
    unique = []
    for race in races:
        if race['id'] in seen:
            ambiguity.append(f'duplicate id {race["id"]}; skipped duplicate from {race.get("sourceFile", "?")}.')
            continue
        seen.add(race['id'])
        unique.append(race)

    for race in unique:
        out = BASE / (race['id'].split('.', 1)[1] + '.json')
        out.write_text(json.dumps(race, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')
        write_meta(out)

    (BASE / 'race_import_ambiguities.md').write_text(
        '# Race Import Ambiguity Report\n\n'
        + ('\n'.join(f'- {a}' for a in ambiguity) if ambiguity else 'No ambiguities detected.\n'),
        encoding='utf-8',
    )
    print(f'Generated {len(unique)} race JSON files.')


if __name__ == '__main__':
    main()
