# PowerShell script to add @rendermode InteractiveServer to pages missing it

$pagesPath = "src/AutoPartShop.Web/Components/Pages"
$files = Get-ChildItem -Path $pagesPath -Name "*.razor" -Recurse

$count = 0

foreach ($file in $files) {
    $fullPath = Join-Path $pagesPath $file
    $content = Get-Content -Path $fullPath -Raw
    
    # Check if file has @page but no @rendermode
    if ($content -match "@page" -and $content -notmatch "@rendermode") {
        Write-Host "Fixing: $file"
        
        # Add @rendermode after @page directive
        $newContent = $content -replace '(@page\s+"[^"]*")', "@`$1`n@rendermode InteractiveServer"
        
        # Save file
        Set-Content -Path $fullPath -Value $newContent -Encoding UTF8
        
        $count++
    }
}

Write-Host "`nFixed $count files"
