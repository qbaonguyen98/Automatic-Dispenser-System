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
$result = $dblink->query("SELECT DispenserID, UID, Data, Timestamp
FROM operation_log
WHERE EventType = 'RFID Scanned'
ORDER BY Timestamp DESC 
LIMIT 30");
$dbdata = array();
while ( $row = $result->fetch_assoc())
{
    $dbdata[]=$row;
}
echo json_encode($dbdata);
    
?>