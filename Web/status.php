<?php
$servername = "localhost";
$username = "root";
$password = "";
$database = "iot";
$dblink = new mysqli($servername, $username, $password, $database);
if ($dblink->connect_errno) {
    printf("Failed to connect to database");
    exit();
    }
$result = $dblink->query("SELECT device_info.DispenserID, 
    device_info.DispenserVolume,
    device_info.SpiritName, 
    device_info.EstimatedRemainingShot, 
    device_info.PreviousUserName, 
    device_info.UserName, 
    device_info.DispenserShot, 
    device_info.DispenserStatus,
    spirit_info.Volume 
FROM `device_info` 
LEFT JOIN `spirit_info` 
ON device_info.SpiritName = spirit_info.SpiritName 
ORDER BY DispenserID ASC");
$dbdata = array();
while ( $row = $result->fetch_assoc())
{
//    $row = $result->fetch_assoc()
    $dbdata[]=$row;
}
echo json_encode($dbdata);
?>