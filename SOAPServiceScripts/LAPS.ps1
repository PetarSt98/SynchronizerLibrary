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

$service=New-WebServiceProxy -uri "https://network.cern.ch/sc/soap/soap.fcgi?v=6&WSDL" -Namespace SOAPNetworkService -Class SOAPNetworkServiceClass
$service.AuthValue = new-object SOAPNetworkService.Auth
$service.AuthValue.token = $service.getAuthToken("pstojkov","GeForce9800GT.","NICE")
# Call to recursive function for set exploration - output file containing IP addresses at C:\temp.txt
$DeviceInfo = $service.getDeviceInfo($SetName1)
$passDict | Add-Member -MemberType NoteProperty -Name "os" -Value $DeviceInfo.OperatingSystem.Name
$passDict | Format-List
