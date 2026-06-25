// ===================== CONFIG =====================
const API_BASE = 'https://techstoreweb.somee.com/api';

// ===================== STATE =====================
let db = {
  products: [],
  orders: [],
  customers: [],
  invoiceCart: [],
  editingProductId: null,
  editingStockId: null,
};

// ===================== API HELPERS =====================
function getAuthHeader() {
  return currentUser && currentUser.token ? { 'Authorization': 'Bearer ' + currentUser.token } : {};
}

async function apiGet(path) {
  try {
    const res = await fetch(API_BASE + path, {
      headers: { 'Content-Type': 'application/json', ...getAuthHeader() }
    });
    if (!res.ok) throw new Error('HTTP ' + res.status);
    return await res.json();
  } catch (e) {
    console.error('GET ' + path, e);
    return null;
  }
}

async function apiPost(path, body) {
  const res = await fetch(API_BASE + path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify(body)
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || err.Message || 'HTTP ' + res.status);
  }
  return await res.json().catch(() => ({}));
}

async function apiPut(path, body) {
  const res = await fetch(API_BASE + path, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() },
    body: JSON.stringify(body)
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.message || err.Message || 'HTTP ' + res.status);
  }
  return await res.json().catch(() => ({}));
}

async function apiDelete(path) {
  const res = await fetch(API_BASE + path, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json', ...getAuthHeader() }
  });
  if (!res.ok) throw new Error('HTTP ' + res.status);
  return true;
}

// ===================== NORMALIZE =====================
function emojiForCategory(cat) {
  const map = { 'Dien thoai': '📱', 'Điện thoại': '📱', 'Laptop': '💻', 'May tinh bang': '📟', 'Máy tính bảng': '📟', 'Phu kien': '🔌', 'Phụ kiện': '🔌', 'Man hinh': '🖥️', 'Màn hình': '🖥️', 'Am thanh': '🎧', 'Âm thanh': '🎧' };
  return map[cat] || '📦';
}

function normProduct(p) {
  return {
    id: p.Id || p.id,
    name: p.Name || p.name || '',
    category: p.Category || p.category || '',
    categoryId: p.CategoryId || p.categoryId || 0,
    cost: p.Cost || p.cost || 0,
    price: p.Price || p.price || 0,
    stock: p.Stock != null ? p.Stock : (p.stock != null ? p.stock : 0),
    brand: p.Brand || p.brand || '',
    desc: p.Desc || p.desc || '',
    emoji: p.Emoji || p.emoji || emojiForCategory(p.Category || p.category),
  };
}

function normOrder(o) {
  return {
    id: o.Id || o.id,
    customerId: o.CustomerId || o.customerId || '',
    productId: o.ProductId || o.productId || '',
    customer: o.Customer || o.customer || '',
    product: o.Product || o.product || '',
    qty: o.Qty || o.qty || 1,
    total: o.Total || o.total || 0,
    status: o.Status || o.status || '',
    date: o.Date || o.date || '',
  };
}

function normCustomer(c) {
  return {
    id: c.Id || c.id,
    name: c.Name || c.name || '',
    phone: c.Phone || c.phone || '',
    email: c.Email || c.email || '',
    address: c.Address || c.address || '',
    orders: c.Orders != null ? c.Orders : (c.orders != null ? c.orders : 0),
    spent: c.Spent != null ? c.Spent : (c.spent != null ? c.spent : 0),
  };
}

// ===================== DATA LOADING =====================
async function loadProducts() {
  const data = await apiGet('/products');
  if (data) db.products = (Array.isArray(data) ? data : (data.data || data.products || [])).map(normProduct);
}

async function loadOrders() {
  const data = await apiGet('/orders');
  if (data) db.orders = (Array.isArray(data) ? data : (data.data || data.orders || [])).map(normOrder);
}

async function loadCustomers() {
  const data = await apiGet('/customers');
  if (data) db.customers = (Array.isArray(data) ? data : (data.data || data.customers || [])).map(normCustomer);
}

// ===================== NAVIGATION =====================
let currentPage = 'dashboard';

function navigate(page) {
  document.querySelectorAll('.page').forEach(p => p.classList.remove('active'));
  document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));
  document.getElementById('page-' + page).classList.add('active');
  const navEl = document.querySelector(`.nav-item[onclick="navigate('${page}')"]`);
  if (navEl) navEl.classList.add('active');
  currentPage = page;
  const titles = { dashboard: 'Dashboard', products: 'Quản lý Sản phẩm', orders: 'Quản lý Đơn hàng', invoice: 'Tạo Hóa đơn', customers: 'Quản lý Khách hàng', inventory: 'Kho hàng', reports: 'Báo cáo & Thống kê', security: 'Bảo mật & Tài khoản', activity: 'Nhật ký Hoạt động' };
  document.getElementById('page-title').textContent = titles[page] || page;
  renderPage(page);
}

function openAddModal() {
  if (currentPage === 'products' || currentPage === 'inventory') openProductModal();
  else if (currentPage === 'customers') openCustomerModal();
  else if (currentPage === 'orders') navigate('invoice');
  else openProductModal();
}

function renderPage(page) {
  if (page === 'dashboard') renderDashboard();
  else if (page === 'products') renderProducts();
  else if (page === 'orders') renderOrders();
  else if (page === 'invoice') renderInvoicePage();
  else if (page === 'customers') renderCustomers();
  else if (page === 'inventory') renderInventory();
  else if (page === 'reports') renderReports();
  else if (page === 'security') renderSecurity();
  else if (page === 'activity') renderActivity();
}

function fmt(n) {
  n = n || 0;
  if (n >= 1000000000) return (n / 1000000000).toFixed(1) + 'B';
  if (n >= 1000000) return (n / 1000000).toFixed(1) + 'M';
  return n.toLocaleString('vi-VN') + 'đ';
}
function fmtFull(n) { return (n || 0).toLocaleString('vi-VN') + 'đ'; }

function showLoading(id, colspan) {
  const el = document.getElementById(id);
  if (!el) return;
  if (el.tagName === 'TBODY' || el.closest('table')) {
    el.innerHTML = `<tr><td colspan="${colspan}" style="text-align:center;color:var(--text3);padding:24px">⟳ Đang tải...</td></tr>`;
  } else {
    el.innerHTML = `<div style="text-align:center;color:var(--text3);padding:24px">⟳ Đang tải...</div>`;
  }
}

function statusBadge(s) {
  const map = { 'Hoàn thành': 'badge-success', 'Đang giao': 'badge-info', 'Chờ xử lý': 'badge-warning', 'Đã hủy': 'badge-danger' };
  return `<span class="badge ${map[s] || 'badge-info'}">${s}</span>`;
}

// ===================== DASHBOARD =====================
async function renderDashboard() {
  showLoading('recent-orders', 6);
  const dash = await apiGet('/dashboard');
  if (dash) {
    document.getElementById('stat-products').textContent = dash.TotalProducts ?? dash.totalProducts ?? db.products.length;
    document.getElementById('stat-revenue').textContent = fmt(dash.TotalRevenue ?? dash.totalRevenue ?? 0);
    document.getElementById('stat-orders').textContent = dash.TotalOrders ?? dash.totalOrders ?? db.orders.length;
    document.getElementById('stat-customers').textContent = dash.TotalCustomers ?? dash.totalCustomers ?? db.customers.length;
  } else {
    const totalRev = db.orders.filter(o => o.status === 'Hoàn thành').reduce((s, o) => s + o.total, 0);
    document.getElementById('stat-products').textContent = db.products.length;
    document.getElementById('stat-revenue').textContent = fmt(totalRev);
    document.getElementById('stat-orders').textContent = db.orders.length;
    document.getElementById('stat-customers').textContent = db.customers.length;
  }

  const pending = db.orders.filter(o => o.status === 'Chờ xử lý').length;
  document.getElementById('pending-badge').textContent = pending;

  // Chart
  const days = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  const vals = (dash && (dash.WeeklyRevenue || dash.weeklyRevenue)) || [12500000, 18900000, 9800000, 31200000, 24600000, 38900000, 22100000];
  const max = Math.max(...vals);
  document.getElementById('revenue-chart').innerHTML = days.map((d, i) => `
    <div class="chart-bar-wrap">
      <div class="chart-bar" style="height:${(vals[i] / max * 100)}%" data-val="${fmtFull(vals[i])}"></div>
      <div class="chart-label">${d}</div>
    </div>`).join('');

  // Low stock
  const low = db.products.filter(p => p.stock <= 5);
  const ll = document.getElementById('low-stock-list');
  ll.innerHTML = low.length === 0
    ? '<div style="color:var(--success);font-size:0.8rem;padding:8px 0">✅ Tất cả sản phẩm đủ hàng</div>'
    : low.map(p => `<div style="display:flex;justify-content:space-between;align-items:center;padding:8px 0;border-bottom:1px solid var(--border);font-size:0.78rem"><span>${p.emoji} ${p.name}</span><span class="badge ${p.stock === 0 ? 'badge-danger' : 'badge-warning'}">${p.stock} còn lại</span></div>`).join('');

  // Recent orders
  const recent = [...db.orders].reverse().slice(0, 5);
  document.getElementById('recent-orders').innerHTML = recent.length === 0
    ? '<tr><td colspan="6" style="text-align:center;color:var(--text3);padding:20px">Chưa có đơn hàng</td></tr>'
    : recent.map(o => `<tr><td class="name">${o.id}</td><td>${o.customer}</td><td>${o.product}</td><td style="color:var(--success);font-weight:600">${fmtFull(o.total)}</td><td>${statusBadge(o.status)}</td><td>${o.date}</td></tr>`).join('');
}

// ===================== PRODUCTS =====================
let productFilter = 'all';
function filterProducts(el, cat) {
  document.querySelectorAll('.filter-chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  productFilter = cat;
  renderProducts();
}

async function renderProducts(search = '') {
  const g = document.getElementById('product-grid');
  g.innerHTML = '<div class="empty"><div class="empty-icon">⟳</div><p>Đang tải...</p></div>';
  await loadProducts();
  let prods = db.products;
  if (productFilter !== 'all') prods = prods.filter(p => p.category === productFilter);
  if (search) prods = prods.filter(p => p.name.toLowerCase().includes(search.toLowerCase()));
  if (prods.length === 0) { g.innerHTML = '<div class="empty"><div class="empty-icon">📦</div><p>Không tìm thấy sản phẩm nào</p></div>'; return; }
  g.innerHTML = prods.map(p => `
    <div class="product-card">
      <div class="product-card-img">${p.emoji}</div>
      <div class="product-card-name">${p.name}</div>
      <div class="product-card-cat">${p.brand} · ${p.category}</div>
      <div class="product-card-price">${fmtFull(p.price)}</div>
      <div class="product-card-stock">Tồn kho: <strong style="color:${p.stock <= 3 ? 'var(--danger)' : p.stock <= 10 ? 'var(--warning)' : 'var(--success)'}">${p.stock}</strong></div>
      <div class="product-card-actions">
        <button class="btn btn-ghost btn-sm" onclick="editProduct('${p.id}')">✏️</button>
        <button class="btn btn-ghost btn-sm" onclick="deleteProduct('${p.id}')">🗑️</button>
      </div>
    </div>`).join('');
}

// ===================== ORDERS =====================
let orderFilter = 'all';
function filterOrders(el, st) {
  document.querySelectorAll('#page-orders .filter-chip').forEach(c => c.classList.remove('active'));
  el.classList.add('active');
  orderFilter = st;
  renderOrders();
}

async function renderOrders() {
  showLoading('orders-tbody', 8);
  await loadOrders();
  let orders = [...db.orders].reverse();
  if (orderFilter !== 'all') orders = orders.filter(o => o.status === orderFilter);
  const tb = document.getElementById('orders-tbody');
  if (orders.length === 0) { tb.innerHTML = '<tr><td colspan="8"><div class="empty"><div class="empty-icon">🛒</div><p>Không có đơn hàng nào</p></div></td></tr>'; return; }
  tb.innerHTML = orders.map(o => `
    <tr>
      <td class="name">${o.id}</td><td>${o.customer}</td><td>${o.product}</td>
      <td style="text-align:center">${o.qty}</td>
      <td style="color:var(--success);font-weight:600">${fmtFull(o.total)}</td>
      <td>${statusBadge(o.status)}</td><td>${o.date}</td>
      <td><select class="form-control" style="padding:3px 6px;font-size:0.7rem;width:120px" onchange="updateOrderStatus('${o.id}', this.value)">
        ${['Chờ xử lý', 'Đang giao', 'Hoàn thành', 'Đã hủy'].map(s => `<option ${s === o.status ? 'selected' : ''}>${s}</option>`).join('')}
      </select></td>
    </tr>`).join('');
}

async function updateOrderStatus(id, status) {
  try {
    await apiPut(`/orders/${id}/status`, { Status: status });
    const o = db.orders.find(o => o.id === id);
    if (o) o.status = status;
    showToast(`Đã cập nhật đơn ${id} → ${status}`, 'success');
    renderOrders();
  } catch (e) { showToast('Lỗi cập nhật: ' + e.message, 'error'); }
}

// ===================== INVOICE =====================
async function renderInvoicePage() {
  document.getElementById('inv-customer').value = '';
  document.getElementById('inv-phone').value = '';
  db.invoiceCart = [];
  await loadProducts();
  renderInvoiceProducts();
  renderInvoiceCart();
}

function renderInvoiceProducts(search = '') {
  let prods = db.products.filter(p => p.stock > 0);
  if (search) prods = prods.filter(p => p.name.toLowerCase().includes(search.toLowerCase()));
  document.getElementById('inv-product-list').innerHTML = prods.map(p => `
    <div style="background:var(--surface2);border:1px solid var(--border);border-radius:8px;padding:10px;cursor:pointer;transition:border-color .2s" onclick="addToCart('${p.id}')" onmouseover="this.style.borderColor='var(--accent)'" onmouseout="this.style.borderColor='var(--border)'">
      <div style="font-size:1.2rem;margin-bottom:4px">${p.emoji}</div>
      <div style="font-size:0.75rem;font-weight:600;color:var(--text);margin-bottom:2px">${p.name}</div>
      <div style="font-size:0.68rem;color:var(--accent)">${fmtFull(p.price)}</div>
      <div style="font-size:0.65rem;color:var(--text3)">Còn: ${p.stock}</div>
    </div>`).join('');
}

function searchInvoiceProducts(v) { renderInvoiceProducts(v); }

function addToCart(pid) {
  const p = db.products.find(p => p.id === pid);
  if (!p) return;
  const item = db.invoiceCart.find(i => i.id === pid);
  if (item) {
    if (item.qty >= p.stock) { showToast('Không đủ hàng trong kho!', 'error'); return; }
    item.qty++;
  } else {
    db.invoiceCart.push({ id: pid, name: p.name, price: p.price, qty: 1, emoji: p.emoji });
  }
  renderInvoiceCart();
}

function removeFromCart(pid) { db.invoiceCart = db.invoiceCart.filter(i => i.id !== pid); renderInvoiceCart(); }

function changeQty(pid, delta) {
  const item = db.invoiceCart.find(i => i.id === pid);
  if (!item) return;
  item.qty += delta;
  if (item.qty <= 0) removeFromCart(pid);
  else renderInvoiceCart();
}

function renderInvoiceCart() { updateInvoice(); }

function updateInvoice() {
  const subtotal = db.invoiceCart.reduce((s, i) => s + i.price * i.qty, 0);
  const disc = parseFloat(document.getElementById('inv-discount').value) || 0;
  const afterDisc = subtotal * (1 - disc / 100);
  const vat = afterDisc * 0.1;
  const total = afterDisc + vat;
  const container = document.getElementById('inv-items');
  container.innerHTML = db.invoiceCart.length === 0
    ? '<div style="color:var(--text3);font-size:0.78rem;padding:20px 0;text-align:center">Chưa có sản phẩm nào</div>'
    : db.invoiceCart.map(i => `
      <div style="display:flex;align-items:center;gap:8px;padding:8px 0;border-bottom:1px solid var(--border);font-size:0.78rem">
        <span>${i.emoji}</span>
        <div style="flex:1"><div>${i.name}</div><div style="color:var(--text3)">${fmtFull(i.price)} × ${i.qty}</div></div>
        <div style="display:flex;align-items:center;gap:4px">
          <button class="btn btn-ghost btn-sm" onclick="changeQty('${i.id}',-1)">−</button>
          <span style="min-width:20px;text-align:center">${i.qty}</span>
          <button class="btn btn-ghost btn-sm" onclick="changeQty('${i.id}',1)">+</button>
          <button class="btn btn-danger btn-sm" onclick="removeFromCart('${i.id}')">✕</button>
        </div>
      </div>`).join('');
  document.getElementById('inv-subtotal').textContent = fmtFull(subtotal);
  document.getElementById('inv-vat').textContent = fmtFull(vat);
  document.getElementById('inv-total').textContent = fmtFull(total);
}

async function saveInvoice() {
  if (db.invoiceCart.length === 0) { showToast('Vui lòng thêm sản phẩm!', 'error'); return; }
  const customer = document.getElementById('inv-customer').value || 'Khách lẻ';
  const phone = document.getElementById('inv-phone').value || '';
  const disc = parseFloat(document.getElementById('inv-discount').value) || 0;
  const subtotal = db.invoiceCart.reduce((s, i) => s + i.price * i.qty, 0);
  const total = Math.round(subtotal * (1 - disc / 100) * 1.1);
  const products = db.invoiceCart.map(i => `${i.name} x${i.qty}`).join(', ');
  const qty = db.invoiceCart.reduce((s, i) => s + i.qty, 0);

  let cust = db.customers.find(c => c.name === customer);
  let customerId = cust ? cust.id : '';
  if (!cust && customer !== 'Khách lẻ') {
    try {
      const nc = await apiPost('/customers', { Name: customer, Phone: phone, Email: '', Address: '', Orders: 0, Spent: 0 });
      customerId = nc.Id || nc.id || '';
    } catch (e) {}
  }

  try {
    const newOrder = await apiPost('/orders', {
      CustomerId: customerId, ProductId: db.invoiceCart.length === 1 ? db.invoiceCart[0].id : '',
      Customer: customer, Product: products, Qty: qty, Total: total,
      Status: 'Chờ xử lý', Date: new Date().toISOString().split('T')[0]
    });
    showToast(`✅ Đã tạo đơn ${newOrder.Id || newOrder.id || ''}`, 'success');
    logActivity('Tạo hóa đơn', `${customer} - ${fmtFull(total)}`);
    for (const item of db.invoiceCart) {
      await apiPost('/stock', { ProductId: item.id, Action: 'out', Qty: item.qty, Note: 'Bán hàng' }).catch(() => {});
    }
    db.invoiceCart = [];
    await Promise.all([loadOrders(), loadProducts()]);
    renderInvoicePage();
  } catch (e) { showToast('Lỗi tạo đơn: ' + e.message, 'error'); }
}

function printInvoice() {
  if (db.invoiceCart.length === 0) { showToast('Hóa đơn trống!', 'error'); return; }
  const customer = document.getElementById('inv-customer').value || 'Khách lẻ';
  const subtotal = db.invoiceCart.reduce((s, i) => s + i.price * i.qty, 0);
  const disc = parseFloat(document.getElementById('inv-discount').value) || 0;
  const discAmt = subtotal * disc / 100;
  const vat = (subtotal - discAmt) * 0.1;
  const total = subtotal - discAmt + vat;
  const rows = db.invoiceCart.map(i => `<tr><td>${i.emoji} ${i.name}</td><td style="text-align:center">${i.qty}</td><td style="text-align:right">${fmtFull(i.price)}</td><td style="text-align:right">${fmtFull(i.price * i.qty)}</td></tr>`).join('');
  const w = window.open('', '_blank');
  w.document.write(`<html><head><title>Hóa đơn TechStore</title><style>body{font-family:monospace;padding:20px;max-width:600px;margin:0 auto}h2{text-align:center}table{width:100%;border-collapse:collapse}th,td{border:1px solid #ccc;padding:8px}th{background:#f5f5f5}.total{font-weight:bold}</style></head><body><h2>🏪 TECHSTORE MANAGER</h2><p><strong>Khách:</strong> ${customer}</p><p><strong>Ngày:</strong> ${new Date().toLocaleString('vi-VN')}</p><table><thead><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead><tbody>${rows}</tbody></table><p style="text-align:right">Tạm tính: ${fmtFull(subtotal)}</p><p style="text-align:right">Giảm giá: ${disc}% (${fmtFull(discAmt)})</p><p style="text-align:right">VAT 10%: ${fmtFull(vat)}</p><p class="total" style="text-align:right">Tổng: ${fmtFull(total)}</p><p style="text-align:center;margin-top:20px">Cảm ơn quý khách! 🙏</p></body></html>`);
  w.print();
}

// ===================== CUSTOMERS =====================
async function renderCustomers() {
  showLoading('customers-tbody', 7);
  await loadCustomers();
  const tb = document.getElementById('customers-tbody');
  if (db.customers.length === 0) { tb.innerHTML = '<tr><td colspan="7" style="text-align:center;color:var(--text3);padding:20px">Chưa có khách hàng</td></tr>'; return; }
  tb.innerHTML = db.customers.map(c => `
    <tr><td class="name">${c.id}</td><td class="name">${c.name}</td><td>${c.phone}</td><td>${c.email || '—'}</td>
    <td style="text-align:center">${c.orders}</td>
    <td style="color:var(--success);font-weight:600">${fmtFull(c.spent)}</td>
    <td><button class="btn btn-danger btn-sm" onclick="deleteCustomer('${c.id}')">🗑️ Xóa</button></td></tr>`).join('');
}

// ===================== INVENTORY =====================
async function renderInventory() {
  await loadProducts();
  const total = db.products.reduce((s, p) => s + p.stock, 0);
  const totalVal = db.products.reduce((s, p) => s + p.stock * p.cost, 0);
  document.getElementById('inventory-stats').innerHTML = `
    <div style="display:grid;grid-template-columns:1fr 1fr;gap:10px">
      <div style="background:var(--surface2);border-radius:8px;padding:12px">
        <div style="font-size:0.65rem;color:var(--text3);margin-bottom:4px">TỔNG TỒN KHO</div>
        <div style="font-family:var(--font-head);font-size:1.4rem;color:var(--accent)">${total}</div>
      </div>
      <div style="background:var(--surface2);border-radius:8px;padding:12px">
        <div style="font-size:0.65rem;color:var(--text3);margin-bottom:4px">GIÁ TRỊ KHO</div>
        <div style="font-family:var(--font-head);font-size:1.2rem;color:var(--success)">${fmt(totalVal)}</div>
      </div>
    </div>`;
  const alerts = db.products.filter(p => p.stock <= 5);
  document.getElementById('inventory-alerts').innerHTML = alerts.length === 0
    ? '<div style="color:var(--success);font-size:0.8rem">✅ Không có cảnh báo</div>'
    : alerts.map(p => `<div style="display:flex;justify-content:space-between;padding:6px 0;border-bottom:1px solid var(--border);font-size:0.78rem"><span>${p.emoji} ${p.name}</span><span class="badge ${p.stock === 0 ? 'badge-danger' : 'badge-warning'}">${p.stock === 0 ? 'Hết hàng' : p.stock + ' còn lại'}</span></div>`).join('');
  document.getElementById('inventory-tbody').innerHTML = db.products.map(p => {
    const profit = p.price - p.cost;
    const pct = p.cost ? ((profit / p.cost) * 100).toFixed(0) : 0;
    return `<tr>
      <td class="name">${p.emoji} ${p.name}</td>
      <td><span class="badge badge-purple">${p.category}</span></td>
      <td><strong style="color:${p.stock === 0 ? 'var(--danger)' : p.stock <= 5 ? 'var(--warning)' : 'var(--success)'}">${p.stock}</strong></td>
      <td>${fmtFull(p.cost)}</td><td>${fmtFull(p.price)}</td>
      <td style="color:var(--success)">${fmtFull(profit)} <span style="font-size:0.65rem;color:var(--text3)">(+${pct}%)</span></td>
      <td>${p.stock === 0 ? '<span class="badge badge-danger">Hết hàng</span>' : p.stock <= 5 ? '<span class="badge badge-warning">Sắp hết</span>' : '<span class="badge badge-success">Còn hàng</span>'}</td>
      <td><button class="btn btn-ghost btn-sm" onclick="openStockModal('${p.id}')">📦 Kho</button> <button class="btn btn-ghost btn-sm" onclick="editProduct('${p.id}')">✏️</button></td>
    </tr>`;
  }).join('');
}

// ===================== REPORTS =====================
async function renderReports() {
  const rep = await apiGet('/reports');
  if (rep) {
    document.getElementById('rep-total-rev').textContent = fmt(rep.TotalRevenue ?? rep.totalRevenue ?? 0);
    document.getElementById('rep-total-profit').textContent = fmt(rep.TotalProfit ?? rep.totalProfit ?? 0);
    document.getElementById('rep-completed').textContent = rep.CompletedOrders ?? rep.completedOrders ?? 0;
    const rate = rep.SuccessRate ?? rep.successRate;
    document.getElementById('rep-rate').textContent = rate !== undefined ? rate + '%' : '—';

    const topProds = rep.TopProducts ?? rep.topProducts ?? [];
    const maxQty = (topProds[0]?.Qty ?? topProds[0]?.qty ?? 1) || 1;
    document.getElementById('top-products').innerHTML = topProds.slice(0, 5).map(t => {
      const name = t.Name || t.name || t.Product || t.product || '';
      const qty = t.Qty || t.qty || 0;
      return `<div style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;font-size:0.75rem;margin-bottom:4px"><span>${name.length > 30 ? name.slice(0, 30) + '...' : name}</span><span style="color:var(--accent)">${qty} SP</span></div><div style="height:6px;background:var(--surface2);border-radius:3px"><div style="height:100%;width:${qty / maxQty * 100}%;background:linear-gradient(90deg,var(--accent),var(--accent2));border-radius:3px"></div></div></div>`;
    }).join('') || '<div style="color:var(--text3);font-size:0.8rem">Không có dữ liệu</div>';

    const catRevs = rep.RevenueByCategory ?? rep.revenueByCategory ?? [];
    const maxCat = (catRevs[0]?.Revenue ?? catRevs[0]?.revenue ?? 1) || 1;
    document.getElementById('cat-revenue').innerHTML = catRevs.slice(0, 5).map(c => {
      const cat = c.Category || c.category || '';
      const rev = c.Revenue || c.revenue || 0;
      return `<div style="margin-bottom:10px"><div style="display:flex;justify-content:space-between;font-size:0.75rem;margin-bottom:4px"><span>${cat}</span><span style="color:var(--success)">${fmtFull(rev)}</span></div><div style="height:6px;background:var(--surface2);border-radius:3px"><div style="height:100%;width:${rev / maxCat * 100}%;background:linear-gradient(90deg,var(--success),var(--accent));border-radius:3px"></div></div></div>`;
    }).join('') || '<div style="color:var(--text3);font-size:0.8rem">Không có dữ liệu</div>';
  } else {
    const done = db.orders.filter(o => o.status === 'Hoàn thành');
    document.getElementById('rep-total-rev').textContent = fmt(done.reduce((s, o) => s + o.total, 0));
    document.getElementById('rep-total-profit').textContent = '—';
    document.getElementById('rep-completed').textContent = done.length;
    document.getElementById('rep-rate').textContent = db.orders.length ? ((done.length / db.orders.length) * 100).toFixed(0) + '%' : '0%';
    document.getElementById('top-products').innerHTML = '<div style="color:var(--text3);font-size:0.8rem">API không khả dụng</div>';
    document.getElementById('cat-revenue').innerHTML = '<div style="color:var(--text3);font-size:0.8rem">API không khả dụng</div>';
  }
}

// ===================== PRODUCT CRUD =====================
function openProductModal(id = null) {
  db.editingProductId = id;
  if (id) {
    const p = db.products.find(p => p.id === id);
    document.getElementById('modal-product-title').textContent = '✏️ Chỉnh sửa sản phẩm';
    document.getElementById('p-name').value = p.name;
    document.getElementById('p-cat').value = p.category;
    document.getElementById('p-cost').value = p.cost;
    document.getElementById('p-price').value = p.price;
    document.getElementById('p-stock').value = p.stock;
    document.getElementById('p-brand').value = p.brand;
    document.getElementById('p-desc').value = p.desc;
  } else {
    document.getElementById('modal-product-title').textContent = '➕ Thêm sản phẩm mới';
    ['p-name', 'p-cost', 'p-price', 'p-stock', 'p-brand', 'p-desc'].forEach(i => document.getElementById(i).value = '');
    document.getElementById('p-cat').value = 'Điện thoại';
  }
  document.getElementById('modal-product').classList.add('open');
}

function editProduct(id) { openProductModal(id); }

async function saveProduct() {
  const name = document.getElementById('p-name').value.trim();
  const cost = parseInt(document.getElementById('p-cost').value) || 0;
  const price = parseInt(document.getElementById('p-price').value) || 0;
  const stock = parseInt(document.getElementById('p-stock').value) || 0;
  if (!name) { showToast('Vui lòng nhập tên sản phẩm!', 'error'); return; }
  if (price === 0) { showToast('Vui lòng nhập giá bán!', 'error'); return; }
  const cat = document.getElementById('p-cat').value;
  const payload = { Name: name, Category: cat, Cost: cost, Price: price, Stock: stock, Brand: document.getElementById('p-brand').value, Desc: document.getElementById('p-desc').value, Emoji: emojiForCategory(cat) };
  try {
    if (db.editingProductId) {
      await apiPut(`/products/${db.editingProductId}`, { ...payload, Id: db.editingProductId });
      showToast('✅ Đã cập nhật sản phẩm!', 'success');
    } else {
      await apiPost('/products', payload);
      showToast('✅ Đã thêm sản phẩm!', 'success');
    }
    logActivity(db.editingProductId ? 'Cập nhật sản phẩm' : 'Thêm sản phẩm', name);
    closeModal('modal-product');
    await loadProducts();
    renderPage(currentPage);
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

async function deleteProduct(id) {
  if (!confirm('Xóa sản phẩm này?')) return;
  try {
    await apiDelete(`/products/${id}`);
    showToast('🗑️ Đã xóa sản phẩm', 'success');
    logActivity('Xóa sản phẩm', id);
    await loadProducts();
    renderPage(currentPage);
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

// ===================== CUSTOMER CRUD =====================
function openCustomerModal() {
  document.getElementById('modal-customer').classList.add('open');
  ['c-name', 'c-phone', 'c-email', 'c-addr'].forEach(i => document.getElementById(i).value = '');
}

async function saveCustomer() {
  const name = document.getElementById('c-name').value.trim();
  const phone = document.getElementById('c-phone').value.trim();
  if (!name || !phone) { showToast('Vui lòng nhập đầy đủ!', 'error'); return; }
  try {
    await apiPost('/customers', { Name: name, Phone: phone, Email: document.getElementById('c-email').value, Address: document.getElementById('c-addr').value, Orders: 0, Spent: 0 });
    showToast('✅ Đã thêm khách hàng!', 'success');
    logActivity('Thêm khách hàng', name);
    closeModal('modal-customer');
    await loadCustomers();
    renderCustomers();
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

async function deleteCustomer(id) {
  if (!confirm('Xóa khách hàng này?')) return;
  try {
    await apiDelete(`/customers/${id}`);
    showToast('🗑️ Đã xóa khách hàng', 'success');
    logActivity('Xóa khách hàng', id);
    await loadCustomers();
    renderCustomers();
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

// ===================== STOCK =====================
function openStockModal(id) {
  db.editingStockId = id;
  const p = db.products.find(p => p.id === id);
  document.getElementById('stock-product-name').textContent = `${p.emoji} ${p.name} (Tồn: ${p.stock})`;
  document.getElementById('stock-qty').value = '';
  document.getElementById('stock-note').value = '';
  document.getElementById('modal-stock').classList.add('open');
}

async function updateStock() {
  const qty = parseInt(document.getElementById('stock-qty').value) || 0;
  if (qty <= 0) { showToast('Nhập số lượng hợp lệ!', 'error'); return; }
  const action = document.getElementById('stock-action').value;
  const p = db.products.find(p => p.id === db.editingStockId);
  if (action === 'out' && qty > p.stock) { showToast('Không đủ hàng!', 'error'); return; }
  try {
    await apiPost('/stock', { ProductId: db.editingStockId, Action: action, Qty: qty, Note: document.getElementById('stock-note').value });
    p.stock += action === 'in' ? qty : -qty;
    showToast(`✅ ${action === 'in' ? 'Nhập' : 'Xuất'} ${qty} SP. Còn: ${p.stock}`, 'success');
    logActivity(action === 'in' ? 'Nhập kho' : 'Xuất kho', `${p.name} x${qty}`);
    closeModal('modal-stock');
    renderPage(currentPage);
  } catch (e) { showToast('Lỗi kho: ' + e.message, 'error'); }
}

// ===================== UTILS =====================
function closeModal(id) { document.getElementById(id).classList.remove('open'); }
document.querySelectorAll('.modal-overlay').forEach(m => m.addEventListener('click', e => { if (e.target === m) m.classList.remove('open'); }));

function showToast(msg, type = '') {
  const t = document.getElementById('toast');
  t.textContent = msg;
  t.className = type ? `show ${type}` : 'show';
  setTimeout(() => t.classList.remove('show', 'success', 'error'), 3000);
}

function globalSearch(val) { if (currentPage === 'products') renderProducts(val); }

// ===================== SECURITY =====================
const PERMISSIONS = {
  admin: { dashboard: true, products: true, orders: true, invoice: true, customers: true, inventory: true, reports: true, security: true, activity: true },
  staff: { dashboard: true, products: true, orders: true, invoice: true, customers: true, inventory: false, reports: false, security: false, activity: false },
  accountant: { dashboard: true, products: false, orders: true, invoice: true, customers: false, inventory: false, reports: true, security: false, activity: false },
};
const ROLE_LABELS = { admin: 'Admin', staff: 'Nhân viên', accountant: 'Kế toán' };
const ROLE_COLORS = { admin: 'role-admin', staff: 'role-staff', accountant: 'role-accountant' };
const ROLE_EMOJI = { admin: '🔴', staff: '🔵', accountant: '🟣' };

let currentUser = null;
let loginAttempts = 0;
let lockUntil = 0;
let activityLog = [];
let allUsers = [];

function logActivity(action, detail = '') {
  if (!currentUser) return;
  activityLog.unshift({ time: new Date().toLocaleString('vi-VN'), user: currentUser.username, fullname: currentUser.fullname, role: currentUser.role, action, detail, id: Date.now() });
  if (activityLog.length > 200) activityLog.pop();
  apiPost('/activity', { User: currentUser.username, Role: currentUser.role, Action: action, Detail: detail }).catch(() => {});
}

function switchLoginTab(tab) {
  document.querySelectorAll('.login-tab').forEach(t => t.classList.remove('active'));
  event.target.classList.add('active');
  document.getElementById('login-form-wrap').style.display = tab === 'login' ? 'block' : 'none';
  document.getElementById('forgot-form-wrap').style.display = tab === 'forgot' ? 'block' : 'none';
  document.getElementById('login-error').classList.remove('show');
}

function fillDemo(user, pass) {
  document.getElementById('login-user').value = user;
  document.getElementById('login-pass').value = pass;
  document.getElementById('login-error').classList.remove('show');
}

function togglePass(id, btn) {
  const el = document.getElementById(id);
  el.type = el.type === 'password' ? 'text' : 'password';
  btn.textContent = el.type === 'password' ? '👁️' : '🙈';
}

async function doLogin() {
  
  const now = Date.now();
  if (lockUntil > now) { document.getElementById('login-attempt-info').textContent = `Khóa ${Math.ceil((lockUntil - now) / 1000)}s`; return; }
  const username = document.getElementById('login-user').value.trim();
  const password = document.getElementById('login-pass').value;
  const err = document.getElementById('login-error');
  try {
    const result = await apiPost('/auth/login', { Username: username, Password: password });
  console.log("LOGIN RESPONSE:", result);
    loginAttempts = 0;
    currentUser = {
      id: result.Id || result.id || result.UserId || result.userId,
      username: result.Username || result.username || username,
      fullname: result.Fullname || result.fullname || result.FullName || result.fullName || username,
      role: (result.Role || result.role || 'staff').toLowerCase(),
      token: result.Token || result.token || result.access_token || '',
      lastLogin: new Date().toLocaleString('vi-VN'),
    };
    err.classList.remove('show');
    document.getElementById('login-screen').classList.add('hidden');
    updateSidebarUser();
    applyPermissions();
    await Promise.all([loadProducts(), loadOrders(), loadCustomers()]);
    logActivity('Đăng nhập', 'Đăng nhập thành công');
    navigate('dashboard');
  } catch (e) {
    loginAttempts++;
    err.textContent = `Sai tên đăng nhập hoặc mật khẩu! (${loginAttempts}/5)`;
    err.classList.add('show');
    if (loginAttempts >= 5) {
      lockUntil = Date.now() + 30000;
      err.textContent = '⛔ Quá nhiều lần thử! Khóa 30 giây.';
      const t = setInterval(() => {
        const r = Math.ceil((lockUntil - Date.now()) / 1000);
        document.getElementById('login-attempt-info').textContent = r > 0 ? `Mở khóa sau ${r}s` : '';
        if (r <= 0) { clearInterval(t); loginAttempts = 0; document.getElementById('login-attempt-info').textContent = ''; err.classList.remove('show'); }
      }, 1000);
    }
  }
}

async function doLogout() {
  if (!confirm('Đăng xuất?')) return;
  logActivity('Đăng xuất', 'Phiên kết thúc');
  currentUser = null;
  document.getElementById('login-user').value = '';
  document.getElementById('login-pass').value = '';
  document.getElementById('login-error').classList.remove('show');
  document.getElementById('login-screen').classList.remove('hidden');
  document.querySelectorAll('.nav-item').forEach(item => {
    item.classList.remove('locked');
    item.querySelector('.lock-icon')?.remove();
  });
}

async function doForgot() {
  const u = document.getElementById('forgot-user').value.trim();
  const err = document.getElementById('forgot-error');
  const res = document.getElementById('forgot-result');
  try {
    await apiPost('/auth/forgot-password', { Username: u });
    err.classList.remove('show');
    res.style.display = 'block';
    res.innerHTML = `✅ Yêu cầu đã gửi!<br><span style="color:var(--text3);font-size:0.7rem">Kiểm tra email của bạn.</span>`;
  } catch (e) {
    err.textContent = 'Không tìm thấy tài khoản!';
    err.classList.add('show');
    res.style.display = 'none';
  }
}

function updateSidebarUser() {
  if (!currentUser) return;
  const avatarMap = { admin: '👑', staff: '👨‍💼', accountant: '📊' };
  document.getElementById('sidebar-avatar').textContent = avatarMap[currentUser.role] || '👤';
  document.getElementById('sidebar-username').textContent = currentUser.fullname;
  document.getElementById('sidebar-role-badge').innerHTML = `<span class="role-badge ${ROLE_COLORS[currentUser.role] || ''}" style="font-size:0.6rem;padding:2px 8px">${ROLE_EMOJI[currentUser.role] || '⚪'} ${ROLE_LABELS[currentUser.role] || currentUser.role}</span>`;
}

function applyPermissions() {
  if (!currentUser) return;
  const perms = PERMISSIONS[currentUser.role] || PERMISSIONS.staff;
  document.querySelectorAll('.nav-item').forEach(item => {
    const match = (item.getAttribute('onclick') || '').match(/navigate\('(\w+)'\)/);
    if (match && perms[match[1]] === false) {
      item.classList.add('locked');
      if (!item.querySelector('.lock-icon')) item.insertAdjacentHTML('beforeend', '<span class="lock-icon">🔒</span>');
      item.setAttribute('onclick', 'showToast("🔒 Bạn không có quyền!","error")');
    }
  });
}

async function renderSecurity() {
  const acc = currentUser;
  const avatarMap = { admin: '👑', staff: '👨‍💼', accountant: '📊' };
  document.getElementById('my-account-info').innerHTML = `
    <div style="display:flex;align-items:center;gap:12px;margin-bottom:16px">
      <div style="width:50px;height:50px;border-radius:50%;background:var(--surface2);display:flex;align-items:center;justify-content:center;font-size:1.8rem;border:2px solid var(--accent)">${avatarMap[acc.role] || '👤'}</div>
      <div>
        <div style="font-family:var(--font-head);font-size:1rem;font-weight:700">${acc.fullname}</div>
        <div style="font-size:0.72rem;color:var(--text3)">@${acc.username}</div>
        <span class="role-badge ${ROLE_COLORS[acc.role] || ''}" style="margin-top:4px;font-size:0.65rem">${ROLE_EMOJI[acc.role] || ''} ${ROLE_LABELS[acc.role] || acc.role}</span>
      </div>
    </div>
    <div style="font-size:0.75rem;color:var(--text2)">
      <div style="padding:6px 0;border-bottom:1px solid var(--border)">🕐 Đăng nhập cuối: <strong>${acc.lastLogin || '—'}</strong></div>
      <div style="padding:6px 0">🔑 Trạng thái: <span class="badge badge-success">Đang hoạt động</span></div>
    </div>`;

  const pages = ['dashboard', 'products', 'orders', 'invoice', 'customers', 'inventory', 'reports', 'security', 'activity'];
  const pageNames = { dashboard: 'Dashboard', products: 'Sản phẩm', orders: 'Đơn hàng', invoice: 'Hóa đơn', customers: 'Khách hàng', inventory: 'Kho hàng', reports: 'Báo cáo', security: 'Bảo mật', activity: 'Nhật ký' };
  document.getElementById('role-permissions-table').innerHTML = `
    <table style="width:100%;border-collapse:collapse;font-size:0.72rem">
      <thead><tr><th style="padding:6px 8px;text-align:left;color:var(--text3);border-bottom:1px solid var(--border)">Chức năng</th><th style="padding:6px 8px;text-align:center;color:var(--accent3)">Admin</th><th style="padding:6px 8px;text-align:center;color:var(--accent)">NV</th><th style="padding:6px 8px;text-align:center;color:var(--accent2)">KT</th></tr></thead>
      <tbody>${pages.map(p => `<tr style="border-bottom:1px solid var(--border)"><td style="padding:6px 8px;color:var(--text2)">${pageNames[p]}</td><td style="text-align:center">${PERMISSIONS.admin[p] ? '✅' : '—'}</td><td style="text-align:center">${PERMISSIONS.staff[p] ? '✅' : '—'}</td><td style="text-align:center">${PERMISSIONS.accountant[p] ? '✅' : '—'}</td></tr>`).join('')}</tbody>
    </table>`;

  const adminSection = document.getElementById('admin-user-section');
  if (currentUser.role === 'admin') {
    const usersData = await apiGet('/users');
    if (usersData) {
      allUsers = (Array.isArray(usersData) ? usersData : (usersData.data || [])).map(u => ({
        id: u.Id || u.id, username: u.Username || u.username, fullname: u.Fullname || u.fullname || u.FullName || u.fullName || '',
        role: (u.Role || u.role || 'staff').toLowerCase(), status: u.Status || u.status || 'active',
        createdAt: u.CreatedAt || u.createdAt || '', lastLogin: u.LastLogin || u.lastLogin || null,
      }));
    }
    adminSection.innerHTML = `
      <div class="section-header"><div><div class="section-title">👥 Quản lý tài khoản hệ thống</div><div class="section-sub">Chỉ Admin mới thấy</div></div><button class="btn btn-primary btn-sm" onclick="openUserModal()">+ Thêm tài khoản</button></div>
      <div class="table-wrap"><table><thead><tr><th>ID</th><th>Tên đăng nhập</th><th>Họ tên</th><th>Vai trò</th><th>Trạng thái</th><th>Đăng nhập cuối</th><th>Thao tác</th></tr></thead>
      <tbody>${allUsers.map(a => `<tr><td style="color:var(--text3)">${a.id}</td><td class="name">@${a.username}</td><td>${a.fullname}</td><td><span class="role-badge ${ROLE_COLORS[a.role] || ''}">${ROLE_EMOJI[a.role] || '⚪'} ${ROLE_LABELS[a.role] || a.role}</span></td><td>${a.status === 'active' ? '<span class="badge badge-success">Hoạt động</span>' : '<span class="badge badge-danger">Bị khóa</span>'}</td><td style="color:var(--text3)">${a.lastLogin || 'Chưa đăng nhập'}</td><td style="display:flex;gap:6px"><button class="btn btn-ghost btn-sm" onclick="editUserModal('${a.id}')">✏️</button>${a.id !== currentUser.id ? `<button class="btn btn-${a.status === 'active' ? 'danger' : 'success'} btn-sm" onclick="toggleUserStatus('${a.id}')">${a.status === 'active' ? '🔒' : '🔓'}</button>` : '<span style="font-size:0.7rem;color:var(--text3);padding:4px 8px">(Bạn)</span>'}</td></tr>`).join('')}</tbody></table></div>`;
  } else {
    adminSection.innerHTML = `<div style="text-align:center;padding:30px;color:var(--text3);font-size:0.8rem">🔒 Chỉ Admin mới quản lý tài khoản</div>`;
  }
}

function openChangePass() {
  ['cp-old', 'cp-new', 'cp-confirm'].forEach(i => document.getElementById(i).value = '');
  document.getElementById('strength-fill').style.width = '0';
  document.getElementById('strength-label').textContent = 'Nhập mật khẩu mới';
  document.getElementById('modal-changepass').classList.add('open');
}

function checkStrength(val) {
  const fill = document.getElementById('strength-fill'), label = document.getElementById('strength-label');
  if (!val) { fill.style.width = '0'; label.textContent = 'Nhập mật khẩu mới'; return; }
  let score = [val.length >= 6, val.length >= 10, /[A-Z]/.test(val), /[0-9]/.test(val), /[^a-zA-Z0-9]/.test(val)].filter(Boolean).length;
  const lv = [{ w: '20%', c: 'var(--danger)', t: 'Rất yếu' }, { w: '40%', c: 'var(--accent3)', t: 'Yếu' }, { w: '60%', c: 'var(--warning)', t: 'Trung bình' }, { w: '80%', c: 'var(--accent)', t: 'Mạnh' }, { w: '100%', c: 'var(--success)', t: 'Rất mạnh' }][Math.max(0, score - 1)];
  fill.style.width = lv.w; fill.style.background = lv.c; label.textContent = lv.t; label.style.color = lv.c;
}

async function doChangePass() {
  const oldP = document.getElementById('cp-old').value;
  const newP = document.getElementById('cp-new').value;
  const confirmP = document.getElementById('cp-confirm').value;
  if (newP.length < 6) { showToast('Mật khẩu mới phải có ít nhất 6 ký tự!', 'error'); return; }
  if (newP !== confirmP) { showToast('Mật khẩu không khớp!', 'error'); return; }
  try {
    await apiPost(`/users/${currentUser.id}/change-password`, { OldPassword: oldP, NewPassword: newP });
    logActivity('Đổi mật khẩu', 'Thành công');
    showToast('✅ Đổi mật khẩu thành công!', 'success');
    closeModal('modal-changepass');
  } catch (e) { showToast('Mật khẩu hiện tại không đúng!', 'error'); }
}

let editingUserId = null;
function openUserModal(id = null) {
  editingUserId = id;
  document.getElementById('modal-user-title').textContent = id ? '✏️ Chỉnh sửa tài khoản' : '➕ Thêm tài khoản';
  document.getElementById('u-pass-group').style.display = id ? 'none' : 'block';
  if (id) {
    const a = allUsers.find(u => u.id === id);
    if (a) { document.getElementById('u-username').value = a.username; document.getElementById('u-fullname').value = a.fullname; document.getElementById('u-role').value = a.role; document.getElementById('u-status').value = a.status; }
  } else {
    ['u-username', 'u-fullname', 'u-password'].forEach(i => document.getElementById(i).value = '');
    document.getElementById('u-role').value = 'staff'; document.getElementById('u-status').value = 'active';
  }
  document.getElementById('modal-user').classList.add('open');
}
function editUserModal(id) { openUserModal(id); }

async function saveUser() {
  const username = document.getElementById('u-username').value.trim();
  const fullname = document.getElementById('u-fullname').value.trim();
  const role = document.getElementById('u-role').value;
  const status = document.getElementById('u-status').value;
  if (!username || !fullname) { showToast('Vui lòng nhập đầy đủ!', 'error'); return; }
  try {
    if (editingUserId) {
      await apiPut(`/users/${editingUserId}`, { Id: editingUserId, Username: username, Fullname: fullname, Role: role, Status: status });
      logActivity('Cập nhật tài khoản', `@${username}`);
      showToast('✅ Đã cập nhật!', 'success');
    } else {
      const password = document.getElementById('u-password').value;
      if (!password || password.length < 6) { showToast('Mật khẩu phải có ít nhất 6 ký tự!', 'error'); return; }
      await apiPost('/users', { Username: username, Password: password, Fullname: fullname, Role: role, Status: status, CreatedAt: new Date().toISOString().split('T')[0] });
      logActivity('Tạo tài khoản', `@${username} (${ROLE_LABELS[role]})`);
      showToast('✅ Đã thêm tài khoản!', 'success');
    }
    closeModal('modal-user');
    renderSecurity();
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

async function toggleUserStatus(id) {
  const acc = allUsers.find(a => a.id === id);
  if (!acc) return;
  try {
    await apiPost(`/users/${id}/toggle-status`, {});
    acc.status = acc.status === 'active' ? 'locked' : 'active';
    logActivity(acc.status === 'locked' ? 'Khóa TK' : 'Mở khóa TK', `@${acc.username}`);
    showToast(`${acc.status === 'active' ? '🔓 Mở khóa' : '🔒 Khóa'} @${acc.username}`, 'success');
    renderSecurity();
  } catch (e) { showToast('Lỗi: ' + e.message, 'error'); }
}

async function renderActivity() {
  const data = await apiGet('/activity');
  if (data) {
    const apiLog = (Array.isArray(data) ? data : (data.data || [])).map(l => ({
      time: l.Time || l.time || l.CreatedAt || l.createdAt || '',
      user: l.User || l.user || '', fullname: l.Fullname || l.fullname || '',
      role: (l.Role || l.role || 'staff').toLowerCase(),
      action: l.Action || l.action || '', detail: l.Detail || l.detail || ''
    }));
    apiLog.forEach(a => { if (!activityLog.find(m => m.time === a.time && m.user === a.user && m.action === a.action)) activityLog.push(a); });
  }
  const tb = document.getElementById('activity-tbody');
  if (activityLog.length === 0) { tb.innerHTML = '<tr><td colspan="5"><div class="empty"><div class="empty-icon">📋</div><p>Chưa có hoạt động</p></div></td></tr>'; return; }
  tb.innerHTML = activityLog.map(l => `
    <tr>
      <td style="color:var(--text3);font-size:0.72rem;white-space:nowrap">${l.time}</td>
      <td class="name">@${l.user}</td>
      <td><span class="role-badge ${ROLE_COLORS[l.role] || ''}" style="font-size:0.62rem">${ROLE_EMOJI[l.role] || '⚪'} ${ROLE_LABELS[l.role] || l.role}</span></td>
      <td>${l.action}</td><td style="color:var(--text3)">${l.detail || '—'}</td>
    </tr>`).join('');
}

async function clearLog() {
  if (!confirm('Xóa toàn bộ nhật ký?')) return;
  try { await apiDelete('/activity'); } catch (e) {}
  activityLog = [];
  renderActivity();
  showToast('🗑️ Đã xóa nhật ký', 'success');
}

// ===================== NAVIGATE WITH PERMISSION =====================
const _navigate = navigate;
navigate = function (page) {
  const perms = currentUser ? (PERMISSIONS[currentUser.role] || PERMISSIONS.staff) : {};
  if (currentUser && perms[page] === false) { showToast('🔒 Bạn không có quyền!', 'error'); return; }
  _navigate(page);
  if (currentUser) {
    const names = { dashboard: 'Dashboard', products: 'Sản phẩm', orders: 'Đơn hàng', invoice: 'Tạo hóa đơn', customers: 'Khách hàng', inventory: 'Kho hàng', reports: 'Báo cáo', security: 'Bảo mật', activity: 'Nhật ký' };
    logActivity('Truy cập trang', names[page] || page);
  }
};
