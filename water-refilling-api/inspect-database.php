<?php
$files = [
    'database-backups/database-before-ownership-20261017-225229.sqlite',
    '../database-backups/database-before-audit-20261017-115857.sqlite',
];

foreach ($files as $file) {
    echo "\n--- $file ---\n";

    if (!file_exists($file)) {
        echo "File not found\n";
        continue;
    }

    try {
        $db = new PDO('sqlite:' . $file);
        $tables = $db->query(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name"
        );

        foreach ($tables as $table) {
            echo $table['name'] . "\n";
        }
    } catch (Throwable $e) {
        echo "Error: " . $e->getMessage() . "\n";
    }
}
