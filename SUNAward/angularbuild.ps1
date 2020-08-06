param(
    [Parameter(Mandatory=$true)][string]$config
)
$prod = ""
$base = "/SUNAward"
if ($config -eq "Prod") {
    $prod = "--prod"
    $base = "/"
}

if ($config -eq "QA") {
    $prod = "--prod"
}

echo "Building angular application..."
echo "> ng build $prod --base-href $base"
& ng build $prod --base-href $base
exit $LastExitCode
