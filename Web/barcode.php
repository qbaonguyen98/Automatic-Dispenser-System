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
$result = $dblink->query("SELECT operation_log.DispenserID, spirit_info.BarcodeID, operation_log.Data, operation_log.Timestamp
FROM operation_log
JOIN spirit_info ON operation_log.Data = spirit_info.SpiritName
WHERE EventType = 'Barcode Scanned'
ORDER BY Timestamp DESC 
LIMIT 30"); 
$dbdata = array();
while ( $row = $result->fetch_assoc())
{
    $dbdata[]=$row;
}
echo json_encode($dbdata);
?>