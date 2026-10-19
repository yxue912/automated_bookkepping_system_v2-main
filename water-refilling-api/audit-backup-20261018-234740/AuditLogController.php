<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\AuditLog;
use Carbon\CarbonImmutable;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Validation\ValidationException;

class AuditLogController extends Controller
{
    public function index(Request $request): JsonResponse
    {
        $filters = $request->validate([
            'from' => ['nullable', 'date_format:Y-m-d'],
            'to' => ['nullable', 'date_format:Y-m-d'],
            'filter' => ['nullable', 'string', 'max:100'],
            'page' => ['nullable', 'integer', 'min:1'],
            // Optional IANA timezone supplied by the desktop client, e.g. America/Los_Angeles.
            'timezone' => ['nullable', 'timezone'],
        ]);

        if (
            isset($filters['from'], $filters['to']) &&
            $filters['from'] > $filters['to']
        ) {
            throw ValidationException::withMessages([
                'to' => ['The end date must be on or after the start date.'],
            ]);
        }

        // Dates selected in the desktop UI are local calendar dates. Convert their
        // boundaries to UTC because audit timestamps are stored in UTC.
        // Configure AUDIT_LOG_TIMEZONE in .env to match the desktop PC timezone,
        // or pass ?timezone=America/Los_Angeles from the desktop client.
        $timezone = $filters['timezone']
            ?? config('app.audit_log_timezone')
            ?? config('app.timezone', 'UTC');

        $query = AuditLog::query()->with('user:id,name,email');

        if (!empty($filters['from'])) {
            $fromUtc = CarbonImmutable::createFromFormat(
                '!Y-m-d',
                $filters['from'],
                $timezone
            )->startOfDay()->utc();

            $query->where('created_at', '>=', $fromUtc->format('Y-m-d H:i:s'));
        }

        if (!empty($filters['to'])) {
            $toUtcExclusive = CarbonImmutable::createFromFormat(
                '!Y-m-d',
                $filters['to'],
                $timezone
            )->addDay()->startOfDay()->utc();

            // Exclusive upper bound safely includes the whole selected local day,
            // including fractional seconds and daylight-saving transitions.
            $query->where('created_at', '<', $toUtcExclusive->format('Y-m-d H:i:s'));
        }

        $filter = trim($filters['filter'] ?? '');

        if ($filter !== '') {
            $query->where(function ($search) use ($filter) {
                $search
                    ->where('event', 'like', "%{$filter}%")
                    ->orWhere('description', 'like', "%{$filter}%")
                    ->orWhere('ip_address', 'like', "%{$filter}%")
                    ->orWhereHas('user', function ($userQuery) use ($filter) {
                        $userQuery
                            ->where('name', 'like', "%{$filter}%")
                            ->orWhere('email', 'like', "%{$filter}%");
                    });
            });
        }

        $logs = $query
            ->orderByDesc('created_at')
            ->orderByDesc('id')
            ->paginate(50);

        return response()->json([
            'success' => true,
            'data' => $logs->items(),
            'current_page' => $logs->currentPage(),
            'last_page' => $logs->lastPage(),
            'per_page' => $logs->perPage(),
            'total' => $logs->total(),
        ]);
    }
}
