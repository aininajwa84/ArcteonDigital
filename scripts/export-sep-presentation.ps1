$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = 'C:\Users\LENOVO\Downloads\AllAttachments\Arcteon Digital-SEP.pptx'
$publicDirectory = Join-Path $repoRoot 'wwwroot/assets/presentations/sep'
$renderDirectory = Join-Path $repoRoot 'bin/SEP-presentation'
New-Item -ItemType Directory -Force -Path $publicDirectory, $renderDirectory | Out-Null
$deckPath = Join-Path $publicDirectory 'arcteon-digital-sep.pptx'
# The supplied presentation may already be open; copy without changing it.
$inputStream = [IO.File]::Open($source, 'Open', 'Read', 'ReadWrite')
try {
    $outputStream = [IO.File]::Create($deckPath)
    try { $inputStream.CopyTo($outputStream) } finally { $outputStream.Dispose() }
} finally { $inputStream.Dispose() }

$powerPoint = New-Object -ComObject PowerPoint.Application
$deck = $null
try {
    $deck = $powerPoint.Presentations.Open($deckPath, $true, $false, $false)
    $width = 1600
    $height = [int][Math]::Round($width * $deck.PageSetup.SlideHeight / $deck.PageSetup.SlideWidth)
    foreach ($slide in $deck.Slides) {
        $slide.Export((Join-Path $renderDirectory ('slide-{0:D2}.png' -f $slide.SlideIndex)), 'PNG', $width, $height)
    }
    $deck.SaveAs((Join-Path $publicDirectory 'arcteon-digital-sep.pdf'), 32)
    Write-Output "Exported $($deck.Slides.Count) slides at ${width}x${height}, plus PDF and original PPTX."
} finally {
    if ($deck) { $deck.Close(); [void][Runtime.InteropServices.Marshal]::ReleaseComObject($deck) }
    # Do not quit PowerPoint: the user may have their own presentation open.
    [void][Runtime.InteropServices.Marshal]::ReleaseComObject($powerPoint)
}
