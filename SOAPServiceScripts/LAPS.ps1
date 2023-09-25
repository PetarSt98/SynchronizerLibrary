param([String] $SetName1 = $(throw "Please specify the set name"),
      [String] $UserName1 = $(throw "Please specify the username"),
      [String] $Password1 = $(throw "Please specify the password"))

$SecurePassword = ConvertTo-SecureString $Password1 -AsPlainText -Force 
$Credential = New-Object System.Management.Automation.PSCredential ($UserName1, $SecurePassword) 
$encodedPass=Get-LapsADPassword -Identity $SetName1 -Credential $Credential 
$securePassword = $encodedPass.Password

# Convert SecureString to BSTR (Basic Security Runtime) and then to a plain text string
$pointer = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
try {
    $plainTextPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($pointer)
} finally {
    [System.Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
}

$passDict = New-Object PSObject
$passDict | Add-Member -MemberType NoteProperty -Name "password" -Value $plainTextPassword

$passDict | Format-List
