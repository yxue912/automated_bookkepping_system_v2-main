<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Invoice;
use Illuminate\Http\JsonResponse;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\DB;
use Illuminate\Validation\Rule;
use Illuminate\Validation\ValidationException;

class InvoiceController extends Controller
{
    public function index(): JsonResponse
    {
        $invoices = Invoice::with('items')
            ->latest()
            ->paginate(15);

        return response()->json($invoices);
    }

    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([
            'invoice_number' => ['required', 'string', 'max:255', 'unique:invoices,invoice_number'],
            'invoice_date' => ['required', 'date'],
            'currency' => ['nullable', 'string', 'max:10'],
            'customer_name' => ['required', 'string', 'max:255'],
            'customer_address' => ['nullable', 'string'],
            'tax_identification_number' => ['nullable', 'string', 'max:255'],
            'prepared_by' => ['nullable', 'string', 'max:255'],
            'prepared_date' => ['nullable', 'date'],
            'reviewed_by' => ['prohibited'],
            'reviewed_date' => ['prohibited'],
            'approved_by' => ['prohibited'],
            'approved_date' => ['prohibited'],
            'items' => ['required', 'array', 'min:1'],
            'items.*.nature_of_service' => ['required', 'string'],
            'items.*.quantity' => ['required', 'numeric', 'decimal:0,2', 'gt:0'],
            'items.*.unit_cost_price' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
            'items.*.amount' => ['required', 'numeric', 'decimal:0,2', 'min:0'],
        ]);
        $this->validateItems($validated['items']);

        $invoice = DB::transaction(function () use ($validated) {
            $invoice = Invoice::create([
                'invoice_number' => $validated['invoice_number'],
                'invoice_date' => $validated['invoice_date'],
                'currency' => $validated['currency'] ?? 'PHP',
                'customer_name' => $validated['customer_name'],
                'customer_address' => $validated['customer_address'] ?? null,
                'tax_identification_number' => $validated['tax_identification_number'] ?? null,
                'prepared_by' => $validated['prepared_by'] ?? null,
                'prepared_date' => $validated['prepared_date'] ?? null,
                'reviewed_by' => $validated['reviewed_by'] ?? null,
                'reviewed_date' => $validated['reviewed_date'] ?? null,
                'approved_by' => $validated['approved_by'] ?? null,
                'approved_date' => $validated['approved_date'] ?? null,
            ]);

            $invoice->items()->createMany($validated['items']);

            return $invoice->load('items');
        });

        return response()->json([
            'message' => 'Invoice created successfully.',
            'data' => $invoice,
        ], 201);
    }

    public function show(Invoice $invoice): JsonResponse
    {
        return response()->json($invoice->load('items'));
    }

    public function update(Request $request, Invoice $invoice): JsonResponse
    {
        $validated = $request->validate([
            'invoice_number' => [
                'sometimes', 'required', 'string', 'max:255',
                Rule::unique('invoices', 'invoice_number')->ignore($invoice->id),
            ],
            'invoice_date' => ['sometimes', 'required', 'date'],
            'currency' => ['sometimes', 'nullable', 'string', 'max:10'],
            'customer_name' => ['sometimes', 'required', 'string', 'max:255'],
            'customer_address' => ['sometimes', 'nullable', 'string'],
            'tax_identification_number' => ['sometimes', 'nullable', 'string', 'max:255'],
            'prepared_by' => ['sometimes', 'nullable', 'string', 'max:255'],
            'prepared_date' => ['sometimes', 'nullable', 'date'],
            'reviewed_by' => ['prohibited'],
            'reviewed_date' => ['prohibited'],
            'approved_by' => ['prohibited'],
            'approved_date' => ['prohibited'],
            'items' => ['sometimes', 'array', 'min:1'],
            'items.*.nature_of_service' => ['required_with:items', 'string'],
            'items.*.quantity' => ['required_with:items', 'numeric', 'decimal:0,2', 'gt:0'],
            'items.*.unit_cost_price' => ['required_with:items', 'numeric', 'decimal:0,2', 'min:0'],
            'items.*.amount' => ['required_with:items', 'numeric', 'decimal:0,2', 'min:0'],
        ]);
        if ($invoice->approved_by !== null) {
            throw ValidationException::withMessages(['invoice' => 'Approved invoices cannot be edited.']);
        }
        if (isset($validated['items'])) {
            $this->validateItems($validated['items']);
        }

        $invoice = DB::transaction(function () use ($validated, $invoice) {
            $invoice->update(collect($validated)->except('items')->all());

            if (array_key_exists('items', $validated)) {
                $invoice->items()->delete();
                $invoice->items()->createMany($validated['items']);
            }

            return $invoice->load('items');
        });

        return response()->json([
            'message' => 'Invoice updated successfully.',
            'data' => $invoice,
        ]);
    }

    public function destroy(Invoice $invoice): JsonResponse
    {
        if ($invoice->approved_by !== null) {
            throw ValidationException::withMessages(['invoice' => 'Approved invoices cannot be deleted.']);
        }
        $invoice->delete();

        return response()->json([
            'message' => 'Invoice deleted successfully.',
        ]);
    }

    private function validateItems(array $items): void
    {
        foreach ($items as $index => $item) {
            $calculated = bcdiv(bcadd(bcmul((string) $item['quantity'], (string) $item['unit_cost_price'], 4), '0.005', 4), '1', 2);
            if (bccomp($calculated, (string) $item['amount'], 2) !== 0) {
                throw ValidationException::withMessages(["items.$index.amount" => 'Amount must equal quantity multiplied by unit cost, rounded to two decimals.']);
            }
        }
    }
}
