<#
    Checks that player-facing strings are localizable and that the English template
    ModuleData\Languages\pfx_strings_en.xml matches the code.

    Usage:  powershell -File tools\check-localization.ps1
    Exit code 0 = clean, 1 = problems listed.

    Reports:
      - MISSING  {=id} used in code but not in the English template
      - STALE    id in the template that the code no longer uses
      - TEXT     template text differs from the default text in code
      - CONFLICT one id used with two different default texts
      - UNTAGGED a setting name, hint, group, perk description or TextObject with no {=id}
      - BAKED    an interpolated $"..." perk description (values must go in {VARIABLES})
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$templatePath = Join-Path $root 'ModuleData\Languages\pfx_strings_en.xml'

$template = @{}
[xml]$xml = [IO.File]::ReadAllText($templatePath, [Text.Encoding]::UTF8)
foreach ($s in $xml.base.strings.string) { $template[$s.id] = $s.text }

$sources = Get-ChildItem -LiteralPath $root -Filter *.cs -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

$problems = New-Object System.Collections.Generic.List[string]
$seen = @{}
$literal = [regex]'"((?:[^"\\]|\\.)*)"(\s*\+)?'
$segment = New-Object regex '^\{=([A-Za-z0-9_]+)\}(.*)$', 'Singleline'

function Unescape([string]$s) {
    return [regex]::Replace($s, '\\(.)', {
        param($m)
        switch ($m.Groups[1].Value) { 'n' { "`n" } 't' { "`t" } default { $m.Groups[1].Value } }
    })
}

$untagged = @(
    @{ Re = 'SettingProperty(Bool|Integer|FloatingInteger|Dropdown|Text|Button)\(\s*"(?!\{=)'; What = 'setting name' },
    @{ Re = 'HintText\s*=\s*"(?!\{=)'; What = 'setting hint' },
    @{ Re = 'SettingPropertyGroup\(\s*"(?!\{=)'; What = 'group name' },
    @{ Re = 'const string \w+ = (\w+ \+ )?"(?>\|?)(?!\{=| )'; What = 'group name constant'; File = 'PerkFixesAndRebalanceSettings.cs' },
    @{ Re = '(primary|secondary)Description:\s*"(?!\{=)'; What = 'perk description' },
    @{ Re = 'new TextObject\(\s*"(?!\{=)'; What = 'TextObject text' }
)

foreach ($file in $sources) {
    $lines = [IO.File]::ReadAllLines($file.FullName)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $where = "$($file.Name):$($i + 1)"
        if ($line.TrimStart().StartsWith('//')) { continue }

        foreach ($rule in $untagged) {
            if ($rule.File -and $file.Name -ne $rule.File) { continue }
            if ($line -match $rule.Re) { $problems.Add("UNTAGGED  $where  $($rule.What) has no {=id}") }
        }
        if ($line -match 'Description:\s*\$"') {
            $problems.Add("BAKED     $where  interpolated description; put the value in a {VARIABLE} and set it with SetTextVariable")
        }

        foreach ($m in $literal.Matches($line)) {
            $raw = Unescape $m.Groups[1].Value
            if (-not $raw.Contains('{=')) { continue }
            # "..." + more (on this line or the next): only the start of the text is known
            $partial = $m.Groups[2].Success -or (
                $line.Substring($m.Index + $m.Length).Trim() -eq '' -and
                $i + 1 -lt $lines.Length -and $lines[$i + 1].TrimStart().StartsWith('+'))
            foreach ($part in $raw.Split('|')) {
                $sm = $segment.Match($part)
                if (-not $sm.Success) { continue }
                $id = $sm.Groups[1].Value
                if (-not $id.StartsWith('PFx')) { continue }   # game ids are translated by the game
                $text = $sm.Groups[2].Value
                if (-not $template.ContainsKey($id)) { $problems.Add("MISSING   $where  $id"); continue }
                $ok = if ($partial) { $template[$id].StartsWith($text) } else { $template[$id] -ceq $text }
                if (-not $ok) { $problems.Add("TEXT      $where  $id  template text differs from the code") }
                if (-not $partial) {
                    if ($seen.ContainsKey($id) -and $seen[$id] -cne $text) { $problems.Add("CONFLICT  $where  $id  used with two different texts") }
                    $seen[$id] = $text
                }
                $seen[$id + '#used'] = $true
            }
        }
    }
}

foreach ($id in $template.Keys) {
    if (-not $seen.ContainsKey($id + '#used')) { $problems.Add("STALE     $id  in the template but not in the code") }
}

if ($problems.Count -eq 0) {
    Write-Output "Localization OK: $($template.Count) strings in the English template, all match the code."
    exit 0
}
$problems | Sort-Object | ForEach-Object { Write-Output $_ }
Write-Output "$($problems.Count) problem(s)."
exit 1
