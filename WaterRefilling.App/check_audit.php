<?php
$db = new PDO("sqlite:database/database.sqlite");
$stmt = $db->query("
    SELECT id, event, description, created_at
    FROM audit_logs
    WHERE event LIKE '%cash_receipt%'
    ORDER BY id DESC
");
print_r($stmt->fetchAll(PDO::FETCH_ASSOC));
?>
