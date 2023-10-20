param([String] $SetName1 = $(throw "Please specify the set name"))

$encodedPass=Get-LapsADPassword -Identity $SetName1

$securePassword = $encodedPass.Password

$passDict = New-Object PSObject
if ($null -eq $securePassword) {
    $passDict | Add-Member -MemberType NoteProperty -Name "password" -Value ""
} else {
    # Convert SecureString to BSTR (Basic Security Runtime) and then to a plain text string
    $pointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $plainTextPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($pointer)
        $passDict | Add-Member -MemberType NoteProperty -Name "password" -Value $plainTextPassword
    } finally {
        [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

$passDict | Format-List
