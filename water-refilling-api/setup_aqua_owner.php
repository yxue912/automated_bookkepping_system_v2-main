<?php

use App\Models\User;
use Illuminate\Contracts\Console\Kernel;
use Illuminate\Support\Facades\DB;
use Spatie\Permission\Models\Permission;
use Spatie\Permission\Models\Role;


require __DIR__ . '/vendor/autoload.php';

$app = require __DIR__ . '/bootstrap/app.php';
$app->make(Kernel::class)->bootstrap();

$connection = DB::connection();
$connection->getPdo();

$email = 'EuclidOwner@water.com';
$name = 'Aqua ARC Owner';

$role = Role::where('name', 'Owner')
    ->where('guard_name', 'web')
    ->firstOrFail();

$permission = Permission::where('name', 'users.manage')
    ->where('guard_name', 'web')
    ->firstOrFail();

if (User::where('email', $email)->exists()) {
    fwrite(STDERR, "STOP: Owner email already exists.\n");
    exit(1);
}

if ($role->hasPermissionTo('users.manage', 'web')) {
    fwrite(STDERR, "STOP: Owner permission state changed; review first.\n");
    exit(1);
}

echo "Target account: {$email}\n";
echo "Existing Owner permissions: " . $role->permissions()->count() . "\n";
echo "Planned changes:\n";
echo "  - Create a separate active Owner account\n";
echo "  - Assign the existing Owner role\n";
echo "  - Add users.manage to the Owner role\n";
echo "No other accounts or permissions will be modified.\n\n";


$password = getenv('AQUA_OWNER_PASSWORD');

if ($password === false || strlen($password) < 12) {
    fwrite(STDERR, "STOP: Password is missing or shorter than 12 characters.\n");
    exit(1);
}


$confirmation = readline("Type CREATE OWNER to apply these changes: ");
if ($confirmation !== 'CREATE OWNER') {
    fwrite(STDERR, "Cancelled. No changes made.\n");
    exit(1);
}

DB::transaction(function () use ($email, $name, $password, $role, $permission) {
    $user = new User();
    $user->name = $name;
    $user->email = $email;
    $user->password = $password;
    $user->is_active = true;
    $user->save();

    $user->syncRoles([$role]);

    $role->givePermissionTo($permission);
});

echo "Owner account created and configured.\n";
echo "Run the verification command next; do not delete this script until verification completes.\n";