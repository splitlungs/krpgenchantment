#$file = Get-Content test.json -raw | ConvertFrom-Json
# foreach($file in $files) { Copy-Item $_ $_.Name.Replace(1, 2) }

$files = Get-ChildItem -Recurse *.json
foreach($file in $files) { Get-Content $file -raw | ConvertFrom-Json 
$file | % {$_.name.Replace(1, 2)}
$file | % {$_.processingHours=4}
$file | % {$_.ingredients.reagent.quantity=4}
$file | % {if($_.enchantments -eq 'durable'){$_.enchantments.durable=2}}
$file | ConvertTo-Json -depth 32 > $file.Name.Replace(1,2)
}