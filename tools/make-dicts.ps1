param([Parameter(Mandatory = $true)][string]$Source)
$root = Split-Path $PSScriptRoot
$out = Join-Path $root 'src\dict'
New-Item -ItemType Directory -Force $out | Out-Null

$rules = @{ ru = '^[а-яё]+$'; en = "^[a-z]+('[a-z]+)?$" }
foreach ($lang in 'ru', 'en') {
  $words = New-Object 'System.Collections.Generic.HashSet[string]'
  foreach ($line in [IO.File]::ReadLines((Join-Path $Source "${lang}_50k.txt"), [Text.Encoding]::UTF8)) {
    $w = $line.Split(' ')[0].ToLowerInvariant()
    if ($w -match $rules[$lang]) {
      [void]$words.Add($w)
      if ($lang -eq 'ru' -and $w.Contains('ё')) { [void]$words.Add($w.Replace('ё', 'е')) }
    }
  }
  $ms = New-Object IO.MemoryStream
  $gz = New-Object IO.Compression.GZipStream($ms, [IO.Compression.CompressionLevel]::Optimal)
  $bytes = [Text.Encoding]::UTF8.GetBytes(([string]::Join("`n", $words)))
  $gz.Write($bytes, 0, $bytes.Length); $gz.Close()
  [IO.File]::WriteAllBytes((Join-Path $out "$lang.txt.gz"), $ms.ToArray())
  "{0}: {1} слов, {2} КБ" -f $lang, $words.Count, [math]::Round($ms.ToArray().Length / 1KB)
}
