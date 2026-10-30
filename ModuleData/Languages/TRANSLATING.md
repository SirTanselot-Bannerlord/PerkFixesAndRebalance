# Translating Perk Fixes and Rebalance

Every player-facing string of this mod (perk descriptions, the MCM menu) is in
`pfx_strings_en.xml` in this folder. The game shows the English text when no translation exists.

## Making a translation

1. Create a folder here for your language, e.g. `DE\`.
2. Copy `pfx_strings_en.xml` into it, rename it (e.g. `pfx_strings_de.xml`), change the
   `<tag language="...">`, and translate each `text="..."`. Keep every `id` as it is.
3. Add a `language_data.xml` next to it. Copy the opening `<LanguageData ...>` line from the game's own
   file for your language, `Modules\Native\ModuleData\Languages\<your language>\language_data.xml`,
   so the language `id` matches the game exactly:

   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <LanguageData id="Deutsch" name="Deutsch" ...copied from the game...>
     <LanguageFile xml_path="DE/pfx_strings_de.xml" />
   </LanguageData>
   ```

Rules for the text:

- Keep `{VALUE}`, `{a0}` and `{HP_PER_POINT}` exactly. They are filled with numbers; you may move them
  within the sentence. `{VALUE}` already carries its sign (`+10`, `-5`), so don't add one.
- `&#10;` is a line break. `{newline}` also works and is safer in some languages.
- MCM group names are nested (`Battle` > `Armor penetration` > `Vandal (Two-Handed, 225)`). A sub-group's
  translation must not contain its parent's translation, or MCM cuts that part out of the name.

## Use the game's official names

The texts name perks, skills and party roles: Minister of Health, Medicine, Surgeon, Boatswain...
Players know them from the game, so use the official translation in your language rather than translating
them again. To find one:

1. Search for the English name in the game's English files, the `.xml` files directly in
   `Modules\<module>\ModuleData\Languages\`, and note its id. For example, in
   `Modules\SandBox\ModuleData\Languages\std_TaleWorlds_CampaignSystem.xml`:
   `<string id="rtTjuJTc" text="Minister of Health" />`
2. Search for that id in the same module's folder for your language, e.g. `...\Languages\DE\`.

| Names | Module | English file |
|---|---|---|
| Skills | Native | `std_TaleWorlds_Core.xml` |
| Perks, and their vanilla descriptions | SandBox | `std_TaleWorlds_CampaignSystem.xml` |
| Party roles (Surgeon, Quartermaster, Scout...) | Native | `std_common_strings_xml.xml` |
| War Sails skills, perks and descriptions | NavalDLC | `std_NavalDLC.xml` |
| War Sails roles (First Mate, Navigator) | NavalDLC | `std_module_strings_xml.xml` |

Any editor with "find in files" will do (VS Code, Notepad++). The vanilla perk descriptions in the same
files are also a good guide to the official wording of phrases like "troops in your formation".
