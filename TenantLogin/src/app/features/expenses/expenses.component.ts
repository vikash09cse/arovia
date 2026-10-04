import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { ApiResult } from '../../core/models/api.models';

const EXPENSE_CATEGORIES = ['Rent', 'Salaries', 'Utilities', 'Supplies', 'Staff advance', 'Other'] as const;
const STAFF_ADVANCE = 'Staff advance';

interface ExpenseItem {
  id: string;
  amount: number;
  expenseOn: string;
  category: string;
  note?: string | null;
  staffUserId?: string | null;
  staffName?: string | null;
  createdAt: string;
}

interface CategoryTotal {
  category: string;
  total: number;
}

interface ExpenseMonth {
  yearMonth: string;
  isReconciled: boolean;
  monthTotal: number;
  categoryBreakdown: CategoryTotal[];
  items: ExpenseItem[];
}

interface StaffOption {
  id: string;
  name: string;
}

interface UserListItem {
  id: string;
  firstName: string;
  lastName: string;
  role: string;
  roleCode: number;
  statusCode: number;
}

interface UserList {
  items: UserListItem[];
  totalCount: number;
}

function currentYearMonth(): string {
  const now = new Date();
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

function shiftYearMonth(yearMonth: string, delta: number): string {
  const [y, m] = yearMonth.split('-').map(Number);
  const d = new Date(y, m - 1 + delta, 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
}

function monthBounds(yearMonth: string): { from: string; to: string } {
  const [y, m] = yearMonth.split('-').map(Number);
  const from = `${yearMonth}-01`;
  const last = new Date(y, m, 0).getDate();
  return { from, to: `${yearMonth}-${String(last).padStart(2, '0')}` };
}

function defaultDateForMonth(yearMonth: string): string {
  const today = new Date();
  const todayYm = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
  if (todayYm === yearMonth) {
    return `${todayYm}-${String(today.getDate()).padStart(2, '0')}`;
  }
  return monthBounds(yearMonth).from;
}

function formatYearMonth(yearMonth: string): string {
  const [y, m] = yearMonth.split('-').map(Number);
  return new Date(y, m - 1, 1).toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
}

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './expenses.component.html',
  styleUrl: './expenses.component.scss'
})
export class ExpensesComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly categories = EXPENSE_CATEGORIES;
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly closing = signal(false);
  readonly error = signal('');
  readonly msg = signal('');
  readonly yearMonth = signal(currentYearMonth());
  readonly isReconciled = signal(false);
  readonly monthTotal = signal(0);
  readonly items = signal<ExpenseItem[]>([]);
  readonly categoryBreakdown = signal<CategoryTotal[]>([]);
  readonly categoryFilter = signal('all');
  readonly staffOptions = signal<StaffOption[]>([]);

  amount = '';
  expenseOn = defaultDateForMonth(currentYearMonth());
  category: string = 'Rent';
  note = '';
  staffUserId = '';

  readonly monthFrom = computed(() => monthBounds(this.yearMonth()).from);
  readonly monthTo = computed(() => monthBounds(this.yearMonth()).to);
  readonly monthLabel = computed(() => formatYearMonth(this.yearMonth()));

  readonly filteredItems = computed(() => {
    const filter = this.categoryFilter();
    const rows = this.items();
    if (filter === 'all') return rows;
    return rows.filter(r => r.category === filter);
  });

  ngOnInit() {
    this.loadStaff();
    this.loadMonth();
  }

  prevMonth() {
    this.setMonth(shiftYearMonth(this.yearMonth(), -1));
  }

  nextMonth() {
    this.setMonth(shiftYearMonth(this.yearMonth(), 1));
  }

  onMonthInput(value: string) {
    if (value) this.setMonth(value);
  }

  setMonth(next: string) {
    this.yearMonth.set(next);
    this.categoryFilter.set('all');
    this.error.set('');
    this.msg.set('');
    this.expenseOn = defaultDateForMonth(next);
    this.loadMonth();
  }

  setCategoryFilter(cat: string) {
    this.categoryFilter.set(cat);
  }

  onCategoryChange() {
    if (this.category !== STAFF_ADVANCE) this.staffUserId = '';
  }

  loadMonth() {
    this.loading.set(true);
    this.error.set('');
    this.api.get<ApiResult<ExpenseMonth>>(`/expenses?yearMonth=${this.yearMonth()}`).subscribe({
      next: res => {
        const data = res.data;
        this.isReconciled.set(!!data?.isReconciled);
        this.monthTotal.set(data?.monthTotal ?? 0);
        this.items.set(data?.items ?? []);
        this.categoryBreakdown.set(data?.categoryBreakdown ?? []);
        this.loading.set(false);
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to load expenses.');
        this.loading.set(false);
      }
    });
  }

  loadStaff() {
    this.api.get<ApiResult<UserList>>('/users?page=1&pageSize=100').subscribe({
      next: res => {
        const items = (res.data?.items ?? [])
          .filter(u => u.statusCode === 1 && [1, 2, 3].includes(u.roleCode))
          .map(u => ({
            id: u.id,
            name: `${u.firstName} ${u.lastName}`.trim()
          }))
          .sort((a, b) => a.name.localeCompare(b.name));
        this.staffOptions.set(items);
      }
    });
  }

  addExpense() {
    this.error.set('');
    this.msg.set('');
    const amount = Number(this.amount);
    if (!(amount > 0)) {
      this.error.set('Amount must be greater than 0.');
      return;
    }
    if (!this.expenseOn || this.expenseOn.slice(0, 7) !== this.yearMonth()) {
      this.error.set(`Date must fall within ${this.monthLabel()}.`);
      return;
    }
    if (this.category === STAFF_ADVANCE && !this.staffUserId) {
      this.error.set('Select a staff member for the advance.');
      return;
    }

    this.saving.set(true);
    const body = {
      amount,
      expenseOn: this.expenseOn,
      category: this.category,
      note: this.note.trim() || null,
      staffUserId: this.category === STAFF_ADVANCE ? this.staffUserId : null
    };

    this.api.post<ApiResult<ExpenseItem>>('/expenses', body).subscribe({
      next: () => {
        this.saving.set(false);
        this.amount = '';
        this.note = '';
        this.expenseOn = defaultDateForMonth(this.yearMonth());
        if (this.category !== STAFF_ADVANCE) this.staffUserId = '';
        this.msg.set('Expense added.');
        this.loadMonth();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to add expense.');
        this.saving.set(false);
      }
    });
  }

  deleteExpense(item: ExpenseItem) {
    if (!confirm('Delete this expense?')) return;
    this.error.set('');
    this.msg.set('');
    this.api.delete<ApiResult<boolean>>(`/expenses/${item.id}`).subscribe({
      next: () => {
        this.msg.set('Expense deleted.');
        this.loadMonth();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to delete expense.');
      }
    });
  }

  closeMonth() {
    if (!confirm(`Close ${this.monthLabel()}? Revenue and expense totals will lock for investor payouts.`)) {
      return;
    }
    this.closing.set(true);
    this.error.set('');
    this.msg.set('');
    this.api.post<ApiResult<unknown>>('/monthly-reconcile/reconcile', {
      yearMonth: this.yearMonth(),
      note: null
    }).subscribe({
      next: () => {
        this.closing.set(false);
        this.msg.set(`Closed ${this.monthLabel()}. Totals are locked.`);
        this.loadMonth();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to close month.');
        this.closing.set(false);
      }
    });
  }

  reopenMonth() {
    this.closing.set(true);
    this.error.set('');
    this.msg.set('');
    this.api.post<ApiResult<boolean>>('/monthly-reconcile/reopen', {
      yearMonth: this.yearMonth()
    }).subscribe({
      next: () => {
        this.closing.set(false);
        this.msg.set(`Reopened ${this.monthLabel()}.`);
        this.loadMonth();
      },
      error: err => {
        this.error.set(err.error?.message ?? 'Unable to reopen month.');
        this.closing.set(false);
      }
    });
  }

  formatMoney(value: number | null | undefined): string {
    const amount = value ?? 0;
    return `₹${amount.toLocaleString('en-IN', { minimumFractionDigits: 0, maximumFractionDigits: 2 })}`;
  }

  formatDate(value: string): string {
    const d = new Date(value.length <= 10 ? value + 'T00:00:00' : value);
    return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
  }
}
