'use strict';

var state = {
    user: null, canteens: [], currentCanteen: null,
    activeView: 'dashboard',
    stats: null, recentOrders: [], topProducts: [],
    orders: [], orderTab: 'kanban',
    menuTab: 'categories',
    categories: [], products: [], selectedCategoryId: null,
    expandedProductId: null,
    modifierGroupMap: {},
    modifierMap: {},
    orderPollInterval: null
};
var allViews = ['viewDashboard','viewOrders','viewMenu','viewCanteen'];

async function bootstrap() {
    try {
        state.user = await api.auth.me();
        if (state.user.role === 'Customer') { window.location.href = 'customer.html'; return; }
        document.getElementById('headerUser').innerText = state.user.fullName || state.user.username;
        state.canteens = await api.canteens.listWithStaff(state.user.id);
        renderCanteenSelect();
        if (state.canteens.length > 0) {
            state.currentCanteen = state.canteens[0];
            document.getElementById('canteenSelect').value = state.currentCanteen.id;
        }
        document.getElementById('canteenSelect').classList.remove('ẩn');
        window.addEventListener('popstate', function() {
            var v = location.hash.slice(1) || 'dashboard';
            if (v !== state.activeView) navigate(v);
        });
        navigate('dashboard');
    } catch(e) {
        if (e.status === 401) window.location.href = 'login.html';
        else setTimeout(function(){ window.location.href = 'login.html'; }, 2000);
    }
}

function renderCanteenSelect() {
    var sel = document.getElementById('canteenSelect');
    sel.innerHTML = state.canteens.map(function(c) { return '<option value="'+c.id+'">'+c.name+'</option>'; }).join('');
    if (state.currentCanteen) sel.value = state.currentCanteen.id;
}

async function onCanteenChange() {
    var id = parseInt(document.getElementById('canteenSelect').value);
    var c = state.canteens.find(function(x) { return x.id === id; });
    if (c) { state.currentCanteen = c; reloadCurrentView(); }
}

async function reloadCurrentView() { navigate(state.activeView); }

function showView(name) {
    allViews.forEach(function(v) { document.getElementById(v).classList.add('ẩn'); });
    document.getElementById(name).classList.remove('ẩn');
}

function setActiveNav(view) {
    var navIds = ['navDashboard','navOrders','navMenu','navCanteen'];
    navIds.forEach(function(id) {
        var el = document.getElementById(id);
        if (!el) return;
        var target = 'nav' + view.charAt(0).toUpperCase() + view.slice(1);
        if (id === target) el.classList.add('đang-chọn');
        else el.classList.remove('đang-chọn');
    });
}

async function navigate(view) {
    if (location.hash.slice(1) !== view) history.pushState(null, '', '#' + view);
    state.activeView = view;
    setActiveNav(view);
    var titles = { dashboard:'Tổng quan', orders:'Đơn hàng', menu:'Thực đơn', canteen:'Căn tin' };
    document.getElementById('headerTitle').innerText = titles[view] || view;
    if (!state.currentCanteen && ['dashboard','orders','menu','canteen'].indexOf(view) !== -1) { showToast('Vui lòng chọn căn tin', 'info'); return; }
    if (view === 'dashboard') { showView('viewDashboard'); await loadDashboard(); }
    else if (view === 'orders') { showView('viewOrders'); await loadOrders(); startOrderPolling(); }
    else if (view === 'menu') { showView('viewMenu'); await loadMenu(); }
    else if (view === 'canteen') { showView('viewCanteen'); await loadCanteenSettings(); }
    if (view !== 'orders') stopOrderPolling();
}

async function loadDashboard() {
    var cid = state.currentCanteen.id;
    try {
        var p1 = api.dashboard.canteenStats(cid);
        var p2 = api.dashboard.recentOrders(cid);
        var p3 = api.dashboard.topProducts(cid);
        state.stats = await p1; state.recentOrders = await p2; state.topProducts = await p3;
        renderDashboard();
    } catch(e) { showToast('Lỗi tải dashboard: ' + (e.message || ''), 'error'); }
}

function renderDashboard() {
    var s = state.stats || {};
    var cards = document.getElementById('statCards');
    cards.innerHTML = '';
    cards.appendChild(statCardElement('Tổng đơn', s.totalOrders||0, 'fa-clipboard-list','nền-xanh-nhạt','color:var(--blue-main)'));
    cards.appendChild(statCardElement('Doanh thu', formatMoney(s.totalRevenue||0), 'fa-chart-line','nền-lục-nhạt','color:#059669'));
    cards.appendChild(statCardElement('Sản phẩm', s.totalProducts||0, 'fa-utensils','nền-hổ-phách-nhạt','color:#d97706'));
    cards.appendChild(statCardElement('Đang chờ', s.pendingOrders||0, 'fa-clock','nền-cam-nhạt','color:#ea580c'));

    var recentTable = document.getElementById('recentOrdersBody').querySelector('tbody');
    recentTable.innerHTML = '';
    var recTmpl = document.getElementById('template-recent-order-row');
    var orders = state.recentOrders || [];
    if (orders.length === 0) {
        recentTable.innerHTML = '<tr><td colspan="5" style="padding:16px;text-align:center;color:var(--text-muted)">Chưa có đơn hàng</td></tr>';
    } else {
        orders.forEach(function(o) {
            var clone = recTmpl.content.cloneNode(true);
            clone.querySelector('.cột-mã-đơn').textContent = '#' + o.id;
            clone.querySelector('.cột-khách').textContent = o.userId;
            clone.querySelector('.cột-tiền').textContent = formatMoney(o.totalAmount);
            clone.querySelector('.cột-thời-gian').textContent = formatDateTime(o.createdAt);
            clone.querySelector('.cột-trạng-thái').appendChild(orderStatusBadgeEl(o.status));
            recentTable.appendChild(clone);
        });
    }

    var topList = document.getElementById('topProductsList');
    topList.innerHTML = '';
    var topTmpl = document.getElementById('template-top-product-item');
    var topProds = state.topProducts || [];
    if (topProds.length === 0) {
        topList.innerHTML = '<p style="font-size:0.75rem;color:var(--text-muted)">Chưa có dữ liệu</p>';
    } else {
        topProds.forEach(function(p, i) {
            var clone = topTmpl.content.cloneNode(true);
            clone.querySelector('.thứ-hạng').textContent = '#' + (i + 1);
            clone.querySelector('.tên-top-sp').textContent = p.name;
            clone.querySelector('.số-bán').textContent = p.soldCount + ' bán';
            topList.appendChild(clone);
        });
    }
}

function statCardElement(label, value, icon, bgClass, colorStyle) {
    var clone = document.getElementById('template-stat-card').content.cloneNode(true);
    clone.querySelector('.biểu-tượng-chỉ-số').classList.add(bgClass);
    clone.querySelector('.biểu-tượng-chỉ-số').style.cssText = colorStyle;
    clone.querySelector('.fa-solid').classList.add(icon);
    clone.querySelector('.nhãn-chỉ-số').textContent = label;
    clone.querySelector('.giá-trị-chỉ-số').textContent = value;
    return clone;
}

function orderStatusBadgeEl(status) {
    var m = { Pending:'nhãn-cam', Preparing:'nhãn-xanh-dương', ReadyForPickup:'nhãn-xanh-ngọc', Delivered:'nhãn-xanh-lá', Cancelled:'nhãn-đỏ' };
    var l = { Pending:'Chờ duyệt', Preparing:'Đang CB', ReadyForPickup:'Chờ giao', Delivered:'Đã giao', Cancelled:'Đã hủy' };
    var span = document.createElement('span');
    span.className = 'nhãn ' + (m[status] || 'nhãn-xám');
    span.textContent = l[status] || status;
    return span;
}

async function loadOrders() {
    var cid = state.currentCanteen.id;
    try { state.orders = await api.orders.listByCanteen(cid, {}); renderOrders(); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}

function stopOrderPolling() { if (state.orderPollInterval) { clearInterval(state.orderPollInterval); state.orderPollInterval = null; } }

function startOrderPolling() {
    stopOrderPolling();
    state.orderPollInterval = setInterval(function() { if (state.activeView === 'orders' && state.currentCanteen) loadOrders(); }, 5000);
}

function setOrderTab(tab) {
    state.orderTab = tab;
    document.getElementById('orderTabKanban').className = 'nút-tab' + (tab==='kanban' ? ' đang-chọn' : '');
    document.getElementById('orderTabCompleted').className = 'nút-tab' + (tab==='completed' ? ' đang-chọn' : '');
    document.getElementById('orderTabCancelled').className = 'nút-tab' + (tab==='cancelled' ? ' đang-chọn' : '');
    renderOrders();
}

function renderOrders() {
    if (state.orderTab === 'kanban') {
        renderKanban();
        document.getElementById('orderKanban').classList.remove('ẩn');
        document.getElementById('orderTable').classList.add('ẩn');
    } else {
        var status = state.orderTab === 'completed' ? 'Delivered' : 'Cancelled';
        renderOrderTable(state.orders.filter(function(o) { return o.status === status; }));
        document.getElementById('orderKanban').classList.add('ẩn');
        document.getElementById('orderTable').classList.remove('ẩn');
    }
}

function renderKanban() {
    var cols = [
        { status: 'Pending', title: 'Chờ duyệt', color: 'viền-cam' },
        { status: 'Preparing', title: 'Đang chế biến', color: 'viền-xanh-dương' },
        { status: 'ReadyForPickup', title: 'Chờ giao', color: 'viền-xanh-ngọc' }
    ];
    var kanban = document.getElementById('orderKanban');
    kanban.innerHTML = '';
    var colTmpl = document.getElementById('template-kanban-column');
    cols.forEach(function(col) {
        var items = state.orders.filter(function(o) { return o.status === col.status; });
        var colClone = colTmpl.content.cloneNode(true);
        colClone.querySelector('.đầu-cột').classList.add(col.color);
        colClone.querySelector('.tiêu-đề-cột').textContent = col.title;
        colClone.querySelector('.đếm-cột').textContent = items.length;

        var listDiv = colClone.querySelector('.danh-sách-cột');
        if (items.length === 0) {
            var empty = document.createElement('p');
            empty.style.cssText = 'font-size:0.75rem;color:var(--text-muted);text-align:center;padding:24px 0';
            empty.textContent = 'Trống';
            listDiv.appendChild(empty);
        } else {
            items.forEach(function(o) { listDiv.appendChild(renderOrderCard(o, col.status)); });
        }
        kanban.appendChild(colClone);
    });
}

function renderOrderCard(o, col) {
    var itemNames = (o.items||[]).map(function(i){return i.productName+' x'+i.quantity;}).join(', ');
    var canMoveLeft = col === 'Preparing' || col === 'ReadyForPickup';
    var canMoveRight = col === 'Pending' || col === 'Preparing';
    var nextStatus = col === 'Pending' ? 'Preparing' : (col === 'Preparing' ? 'ReadyForPickup' : 'Delivered');
    var prevStatus = col === 'Preparing' ? 'Pending' : (col === 'ReadyForPickup' ? 'Preparing' : null);

    var clone = document.getElementById('template-order-card-kanban').content.cloneNode(true);
    clone.querySelector('.mã-đơn-kanban').textContent = '#' + o.id;
    clone.querySelector('.giờ-đơn-kanban').textContent = formatDateTime(o.createdAt);
    clone.querySelector('.món-đơn-kanban').textContent = itemNames;
    clone.querySelector('.tổng-tiền-kanban').textContent = formatMoney(o.totalAmount);

    var paySpan = clone.querySelector('.trạng-thái-thanh-toán-kanban');
    if (o.paymentStatus === 'Paid') { paySpan.style.color = '#059669'; paySpan.textContent = 'Đã TT'; }
    else { paySpan.style.color = '#ef4444'; paySpan.textContent = 'Chưa TT'; }

    clone.querySelector('.mũi-tên-trái').style.display = canMoveLeft ? '' : 'none';
    clone.querySelector('.mũi-tên-phải').style.display = canMoveRight ? '' : 'none';
    clone.querySelector('.giao-hàng').style.display = col === 'ReadyForPickup' ? '' : 'none';

    if (canMoveLeft) clone.querySelector('.mũi-tên-trái').addEventListener('click', function() { moveOrder(o.id, prevStatus); });
    if (canMoveRight) clone.querySelector('.mũi-tên-phải').addEventListener('click', function() { moveOrder(o.id, nextStatus); });
    if (col === 'ReadyForPickup') clone.querySelector('.giao-hàng').addEventListener('click', function() { moveOrder(o.id, 'Delivered'); });
    clone.querySelector('.hủy-đơn').addEventListener('click', function() { cancelOrder(o.id); });

    var paymentBtn = clone.querySelector('.thanh-toán-chưa');
    paymentBtn.addEventListener('click', function() { toggleOrderPayment(o.id, o.paymentStatus); });
    if (o.paymentStatus === 'Paid') {
        paymentBtn.className = 'nút-kanban-nhỏ thanh-toán-đã';
        paymentBtn.querySelector('i').className = 'fa-solid fa-money-bill-wave';
    }

    return clone;
}

function renderOrderTable(list) {
    var tbody = document.getElementById('orderTableBody').querySelector('tbody');
    tbody.innerHTML = '';
    if (list.length === 0) {
        tbody.innerHTML = '<tr><td colspan="6" style="padding:16px;text-align:center;color:var(--text-muted)">Không có đơn</td></tr>';
        return;
    }
    var tmpl = document.getElementById('template-completed-order-row');
    list.forEach(function(o) {
        var clone = tmpl.content.cloneNode(true);
        var items = (o.items||[]).map(function(i){return i.productName+' x'+i.quantity;}).join(', ');
        clone.querySelector('.cột-mã-đơn').textContent = '#' + o.id;
        clone.querySelector('.cột-khách').textContent = o.userId;
        clone.querySelector('.cột-món').textContent = items;
        clone.querySelector('.cột-tiền').textContent = formatMoney(o.totalAmount);
        clone.querySelector('.cột-trạng-thái').appendChild(orderStatusBadgeEl(o.status));
        clone.querySelector('.cột-thời-gian').textContent = formatDateTime(o.createdAt);
        tbody.appendChild(clone);
    });
}

async function moveOrder(id, status) {
    try { await api.orders.updateStatus(id, { status: status }); await loadOrders(); showToast('Cập nhật thành công','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}

async function cancelOrder(id) { if (!confirm('Hủy đơn #'+id+'?')) return; await moveOrder(id, 'Cancelled'); }

async function toggleOrderPayment(id, current) {
    var newStatus = current === 'Paid' ? 'Unpaid' : 'Paid';
    try { await api.orders.updatePayment(id, { paymentStatus: newStatus }); await loadOrders(); showToast('Đã cập nhật thanh toán','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}

async function loadMenu() {
    try { state.categories = await api.categories.list(state.currentCanteen.id); state.products = await api.products.list({ canteenId: state.currentCanteen.id }); renderMenu(); } catch(e) { showToast('Lỗi tải menu: '+(e.message||''),'error'); }
}

function renderMenu() { if (state.menuTab === 'categories') renderCategoriesTable(); else renderProductsPanel(); }

function setMenuTab(tab) {
    state.menuTab = tab;
    document.getElementById('menuTabCat').className = 'nút-tab' + (tab==='categories' ? ' đang-chọn' : '');
    document.getElementById('menuTabProd').className = 'nút-tab' + (tab==='products' ? ' đang-chọn' : '');
    document.getElementById('menuCategoriesPanel').classList.toggle('ẩn', tab !== 'categories');
    document.getElementById('menuProductsPanel').classList.toggle('ẩn', tab !== 'products');
    renderMenu();
}

function renderCategoriesTable() {
    var tbody = document.getElementById('categoriesTableBody').querySelector('tbody');
    tbody.innerHTML = '';
    if (state.categories.length === 0) {
        tbody.innerHTML = '<tr><td colspan="4" style="padding:24px;text-align:center;color:var(--text-muted)">Chưa có danh mục</td></tr>';
        return;
    }
    var tmpl = document.getElementById('template-category-row');
    state.categories.forEach(function(cat) {
        var clone = tmpl.content.cloneNode(true);
        var count = state.products.filter(function(p) { return p.categoryId === cat.id; }).length;
        clone.querySelector('.cột-tên-dm').textContent = cat.name;
        clone.querySelector('.cột-thứ-tự').textContent = cat.displayOrder;
        clone.querySelector('.cột-số-món').textContent = count + ' món';
        clone.querySelector('.nút-sửa-dm').addEventListener('click', function() { openCategoryForm(cat.id); });
        clone.querySelector('.nút-xóa-dm').addEventListener('click', function() { deleteCategory(cat.id, count); });
        tbody.appendChild(clone);
    });
}

function renderProductsPanel() {
    var tabs = document.getElementById('menuCategories');
    if (!state.selectedCategoryId && state.categories.length > 0) state.selectedCategoryId = state.categories[0].id;
    tabs.innerHTML = '';
    var tabTmpl = document.getElementById('template-category-tab-admin');
    state.categories.forEach(function(cat) {
        var clone = tabTmpl.content.cloneNode(true);
        var btn = clone.querySelector('.nút-tab');
        btn.textContent = cat.name;
        if (cat.id === state.selectedCategoryId) btn.classList.add('đang-chọn');
        btn.addEventListener('click', function() { filterMenuCategory(cat.id); });
        tabs.appendChild(clone);
    });

    var filtered = state.products;
    if (state.selectedCategoryId) filtered = filtered.filter(function(p) { return p.categoryId === state.selectedCategoryId; });
    var container = document.getElementById('menuProducts');
    container.innerHTML = '';
    if (filtered.length === 0) {
        container.innerHTML = '<p style="text-align:center;color:var(--text-muted);font-size:0.75rem;padding:32px 0">Chưa có sản phẩm</p>';
        return;
    }
    var prodTmpl = document.getElementById('template-product-row-admin');
    filtered.forEach(function(p) {
        var clone = prodTmpl.content.cloneNode(true);
        var img = clone.querySelector('.ảnh-sp-nhỏ img');
        img.src = imageUrl(p.imageUrl);
        img.onerror = function() { this.style.display = 'none'; };
        clone.querySelector('.tên-sp').textContent = p.name;

        var statusBtn = clone.querySelector('.nút-trạng-thái-sp');
        if (p.status === 'Available') { statusBtn.className = 'nhãn nhãn-xanh-lá'; statusBtn.textContent = 'Còn hàng'; statusBtn.style.fontSize = '0.625rem'; statusBtn.style.cursor = 'pointer'; }
        else { statusBtn.className = 'nhãn nhãn-đỏ'; statusBtn.textContent = 'Hết hàng'; statusBtn.style.fontSize = '0.625rem'; statusBtn.style.cursor = 'pointer'; }
        statusBtn.addEventListener('click', function(e) { e.stopPropagation(); toggleProductStatus(p.id, p.status); });

        clone.querySelector('.giá-sp').innerHTML = formatMoney(p.basePriceAmount) + ' &middot; ' + p.soldCount + ' bán';

        clone.querySelector('.nút-sửa-sp').addEventListener('click', function() { openProductForm(p.id); });
        clone.querySelector('.nút-xóa-sp').addEventListener('click', function() { deleteProduct(p.id); });
        clone.querySelector('.nút-mở-rộng-sp').addEventListener('click', function() { toggleExpanded(p.id); });

        var modsDiv = clone.querySelector('.thân-sản-phẩm');
        modsDiv.id = 'productMods' + p.id;
        modsDiv.querySelector('.danh-sách-mg-sp').id = 'mgList' + p.id;
        modsDiv.querySelector('.nút-thêm-nhóm-sp').addEventListener('click', function() { openMgForm(p.id); });

        container.appendChild(clone);
    });
}

async function toggleProductStatus(productId, currentStatus) {
    var newStatus = currentStatus === 'Available' ? 'OutOfStock' : 'Available';
    try { await api.products.updateStatus(productId, { status: newStatus }); await loadMenu(); showToast(newStatus === 'Available' ? 'Đã mở bán lại' : 'Đã đánh dấu hết hàng', 'success'); } catch(e) { showToast('Lỗi: ' + (e.message || ''), 'error'); }
}

async function toggleExpanded(productId) {
    if (state.expandedProductId === productId) {
        document.getElementById('productMods'+productId).classList.add('ẩn'); state.expandedProductId = null;
    } else {
        if (state.expandedProductId) document.getElementById('productMods'+state.expandedProductId).classList.add('ẩn');
        state.expandedProductId = productId;
        document.getElementById('productMods'+productId).classList.remove('ẩn');
        await loadModifierGroups(productId);
    }
}

async function loadModifierGroups(productId) {
    try {
        var groups = await api.modifierGroups.list(productId);
        state.modifierGroupMap = {}; state.modifierMap = {};
        groups.forEach(function(g) { state.modifierGroupMap[g.id] = g; (g.modifiers || []).forEach(function(m) { state.modifierMap[m.id] = m; }); });
        var container = document.getElementById('mgList'+productId);
        container.innerHTML = '';
        if (groups.length === 0) {
            container.innerHTML = '<p style="color:var(--text-muted);font-size:0.75rem">Chưa có nhóm modifier</p>';
            return;
        }
        var groupTmpl = document.getElementById('template-modifier-group-admin');
        var optTmpl = document.getElementById('template-modifier-option-admin');
        groups.forEach(function(g) {
            var gClone = groupTmpl.content.cloneNode(true);
            gClone.querySelector('.tên-nhóm-mg').textContent = g.name;
            gClone.querySelector('.thông-tin-mg').textContent = (g.required?'Bắt buộc':'Tùy chọn') + ' \u00b7 Tối đa ' + g.maxSelected;
            gClone.querySelector('.nút-sửa-mg').addEventListener('click', function() { openMgFormForEdit(g.id, productId); });
            gClone.querySelector('.nút-xóa-mg').addEventListener('click', function() { deleteModifierGroup(g.id); });
            gClone.querySelector('.nút-thêm-option-mg').addEventListener('click', function() { openModForm(g.id); });

            var optList = gClone.querySelector('.danh-sách-option-mg');
            (g.modifiers || []).forEach(function(m) {
                var mClone = optTmpl.content.cloneNode(true);
                var nameSpan = mClone.querySelector('.tên-option-admin');
                if (m.isDefault) {
                    nameSpan.innerHTML = m.name + ' <span style="font-size:0.625rem;color:var(--blue-main);font-weight:700">(mặc định)</span>';
                } else {
                    nameSpan.textContent = m.name;
                }
                mClone.querySelector('.giá-option-admin').textContent = m.priceAmount > 0 ? '+' + formatMoney(m.priceAmount) : 'Miễn phí';
                mClone.querySelector('.nút-sửa-option-admin').addEventListener('click', function() { openModForm(g.id, m.id); });
                mClone.querySelector('.nút-xóa-option-admin').addEventListener('click', function() { deleteModifier(m.id); });
                optList.appendChild(mClone);
            });

            container.appendChild(gClone);
        });
    } catch(e) { showToast('Lỗi tải modifier: '+(e.message||''),'error'); }
}

function filterMenuCategory(id) { state.selectedCategoryId = id; renderProductsPanel(); }

function openCategoryForm(editId) {
    document.getElementById('catEditId').value = editId || '';
    if (editId) {
        var c = state.categories.find(function(x) { return x.id === editId; });
        if (c) { document.getElementById('catName').value = c.name; document.getElementById('catOrder').value = c.displayOrder; }
        document.getElementById('catModalTitle').innerText = 'Sửa danh mục';
    } else {
        document.getElementById('catName').value = ''; document.getElementById('catOrder').value = '0';
        document.getElementById('catModalTitle').innerText = 'Thêm danh mục';
    }
    document.getElementById('categoryModal').classList.remove('ẩn');
}
function closeCategoryForm() { document.getElementById('categoryModal').classList.add('ẩn'); }
async function saveCategory(e) {
    e.preventDefault();
    var id = document.getElementById('catEditId').value;
    var data = { name: document.getElementById('catName').value.trim(), displayOrder: parseInt(document.getElementById('catOrder').value)||0 };
    try { if (id) await api.categories.update(parseInt(id), data); else await api.categories.create(state.currentCanteen.id, data); closeCategoryForm(); await loadMenu(); showToast('Đã lưu','success'); } catch(er) { showToast('Lỗi: '+(er.message||''),'error'); }
}
async function deleteCategory(id, count) {
    count = count || state.products.filter(function(p) { return p.categoryId === id; }).length;
    if (count > 0) { showToast('Không thể xóa danh mục đang có '+count+' sản phẩm. Vui lòng xóa sản phẩm trước.','error'); return; }
    if (!confirm('Xóa danh mục này?')) return;
    try { await api.categories.delete(id); await loadMenu(); showToast('Đã xóa','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}

function openProductForm(editId) {
    document.getElementById('prodEditId').value = editId || '';
    if (editId) {
        var p = state.products.find(function(x) { return x.id === editId; });
        if (p) { document.getElementById('prodName').value = p.name; document.getElementById('prodDesc').value = p.description || ''; document.getElementById('prodPrice').value = p.basePriceAmount; document.getElementById('prodCategory').value = p.categoryId; document.getElementById('prodStatus').value = p.status; }
        document.getElementById('prodModalTitle').innerText = 'Sửa món';
    } else {
        document.getElementById('prodName').value = ''; document.getElementById('prodDesc').value = ''; document.getElementById('prodPrice').value = ''; document.getElementById('prodStatus').value = 'Available';
        document.getElementById('prodModalTitle').innerText = 'Thêm món';
    }
    document.getElementById('prodImage').value = '';
    document.getElementById('prodCategory').innerHTML = state.categories.map(function(c) { return '<option value="'+c.id+'">'+c.name+'</option>'; }).join('');
    document.getElementById('productModal').classList.remove('ẩn');
}
function closeProductForm() { document.getElementById('productModal').classList.add('ẩn'); }
async function saveProduct(e) {
    e.preventDefault();
    var id = document.getElementById('prodEditId').value;
    var file = document.getElementById('prodImage').files[0];
    try {
        if (id) {
            await api.products.update(parseInt(id), { name: document.getElementById('prodName').value.trim(), description: document.getElementById('prodDesc').value.trim() || null, basePriceAmount: parseFloat(document.getElementById('prodPrice').value)||0, categoryId: parseInt(document.getElementById('prodCategory').value), status: document.getElementById('prodStatus').value });
            if (file) { var fd = new FormData(); fd.append('image', file); await api.products.uploadImage(parseInt(id), fd); }
        } else {
            var fd = new FormData(); fd.append('name', document.getElementById('prodName').value.trim()); fd.append('description', document.getElementById('prodDesc').value.trim()); fd.append('basePriceAmount', parseFloat(document.getElementById('prodPrice').value)||0); fd.append('categoryId', parseInt(document.getElementById('prodCategory').value)); fd.append('status', document.getElementById('prodStatus').value);
            if (file) fd.append('image', file);
            await api.products.create(state.currentCanteen.id, fd);
        }
        closeProductForm(); await loadMenu(); showToast('Đã lưu','success');
    } catch(er) { showToast('Lỗi: '+(er.message||''),'error'); }
}
async function deleteProduct(id) { if (!confirm('Xóa sản phẩm này?')) return; try { await api.products.delete(id); await loadMenu(); showToast('Đã xóa','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); } }

function openMgForm(productId) { openMgFormForEdit(null, productId); }
function openMgFormForEdit(groupId, productId) {
    document.getElementById('mgEditId').value = groupId || ''; document.getElementById('mgProductId').value = productId;
    document.getElementById('mgModalTitle').innerText = groupId ? 'Sửa nhóm' : 'Thêm nhóm';
    if (!groupId) { document.getElementById('mgName').value = ''; document.getElementById('mgRequired').value = 'false'; document.getElementById('mgMaxSelected').value = '1'; }
    else { var mg = state.modifierGroupMap[groupId]; document.getElementById('mgName').value = mg ? mg.name || '' : ''; document.getElementById('mgRequired').value = mg && mg.required ? 'true' : 'false'; document.getElementById('mgMaxSelected').value = mg ? mg.maxSelected || 1 : 1; }
    document.getElementById('mgModal').classList.remove('ẩn');
}
function closeMgForm() { document.getElementById('mgModal').classList.add('ẩn'); }
async function saveModifierGroup(e) {
    e.preventDefault();
    var id = document.getElementById('mgEditId').value;
    var data = { name: document.getElementById('mgName').value.trim(), required: document.getElementById('mgRequired').value === 'true', maxSelected: parseInt(document.getElementById('mgMaxSelected').value)||1 };
    var pid = parseInt(document.getElementById('mgProductId').value);
    try { if (id) await api.modifierGroups.update(parseInt(id), data); else await api.modifierGroups.create(pid, data); closeMgForm(); await toggleExpanded(state.expandedProductId); showToast('Đã lưu','success'); } catch(er) { showToast('Lỗi: '+(er.message||''),'error'); }
}
async function deleteModifierGroup(id) { if (!confirm('Xóa nhóm modifier này và tất cả options?')) return; try { await api.modifierGroups.delete(id); await toggleExpanded(state.expandedProductId); showToast('Đã xóa','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); } }

function openModForm(groupId, editId) {
    document.getElementById('modEditId').value = editId || ''; document.getElementById('modGroupId').value = groupId;
    document.getElementById('modModalTitle').innerText = editId ? 'Sửa option' : 'Thêm option';
    if (!editId) { document.getElementById('modName').value = ''; document.getElementById('modPrice').value = '0'; document.getElementById('modDefault').value = 'false'; }
    else { var mod = state.modifierMap[editId]; document.getElementById('modName').value = mod ? mod.name || '' : ''; document.getElementById('modPrice').value = mod ? mod.priceAmount || 0 : 0; document.getElementById('modDefault').value = mod && mod.isDefault ? 'true' : 'false'; }
    document.getElementById('modModal').classList.remove('ẩn');
}
function closeModForm() { document.getElementById('modModal').classList.add('ẩn'); }
async function saveModifier(e) {
    e.preventDefault();
    var id = document.getElementById('modEditId').value;
    var data = { name: document.getElementById('modName').value.trim(), priceAmount: parseFloat(document.getElementById('modPrice').value)||0, isDefault: document.getElementById('modDefault').value === 'true' };
    var gid = parseInt(document.getElementById('modGroupId').value);
    try { if (id) await api.modifiers.update(parseInt(id), data); else await api.modifiers.create(gid, data); closeModForm(); await toggleExpanded(state.expandedProductId); showToast('Đã lưu','success'); } catch(er) { showToast('Lỗi: '+(er.message||''),'error'); }
}
async function deleteModifier(id) { if (!confirm('Xóa option này?')) return; try { await api.modifiers.delete(id); await toggleExpanded(state.expandedProductId); showToast('Đã xóa','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); } }

async function loadCanteenSettings() {
    try {
        var c = await api.canteens.get(state.currentCanteen.id);
        document.getElementById('canteenName').value = c.name; document.getElementById('canteenAddr').value = c.address;
        document.getElementById('canteenEmail').value = c.email; document.getElementById('canteenPhone').value = c.phoneNumber;
        document.getElementById('canteenStatus').value = c.status;
        renderOperatingHours(c.operatingHours || []);
    } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}
async function saveCanteen(e) {
    e.preventDefault();
    try {
        await api.canteens.update(state.currentCanteen.id, { name: document.getElementById('canteenName').value.trim(), address: document.getElementById('canteenAddr').value.trim(), email: document.getElementById('canteenEmail').value.trim(), phoneNumber: document.getElementById('canteenPhone').value.trim() });
        await api.canteens.updateStatus(state.currentCanteen.id, { status: document.getElementById('canteenStatus').value });
        await saveOperatingHours();
        showToast('Đã lưu thông tin căn tin','success');
    } catch(er) { showToast('Lỗi: '+(er.message||''),'error'); }
}
async function uploadCanteenImage(input) {
    if (!input.files[0]) return;
    try { var fd = new FormData(); fd.append('image', input.files[0]); await api.canteens.uploadImage(state.currentCanteen.id, fd); showToast('Đã cập nhật ảnh','success'); } catch(e) { showToast('Lỗi: '+(e.message||''),'error'); }
}

function renderOperatingHours(hours) {
    var days = ['Chủ nhật','Thứ Hai','Thứ Ba','Thứ Tư','Thứ Năm','Thứ Sáu','Thứ Bảy'];
    var grid = document.getElementById('operatingHoursGrid');
    grid.innerHTML = '';
    var tmpl = document.getElementById('template-operating-hours-row');
    days.forEach(function(name, i) {
        var h = (hours||[]).find(function(x) { return x.dayOfWeek === DAY_NAMES_EN[i]; }) || {};
        var clone = tmpl.content.cloneNode(true);
        clone.querySelector('.ngày-giờ').textContent = name;
        clone.querySelector('[data-field="open"]').value = h.openTime || '';
        clone.querySelector('[data-field="open"]').dataset.day = i;
        clone.querySelector('[data-field="close"]').value = h.closeTime || '';
        clone.querySelector('[data-field="close"]').dataset.day = i;
        var cb = clone.querySelector('[data-field="closed"]');
        cb.dataset.day = i;
        if (h.isClosed) cb.checked = true;
        grid.appendChild(clone);
    });
}
async function saveOperatingHours() {
    var hours = [];
    document.querySelectorAll('#operatingHoursGrid > div').forEach(function(div) {
        var idx = parseInt(div.querySelector('[data-day]').dataset.day);
        hours.push({ dayOfWeek: DAY_NAMES_EN[idx], openTime: div.querySelector('[data-field="open"]').value || null, closeTime: div.querySelector('[data-field="close"]').value || null, isClosed: div.querySelector('[data-field="closed"]').checked });
    });
    try { await api.operatingHours.update(state.currentCanteen.id, { hours: hours }); } catch(e) { showToast('Lỗi lưu giờ: '+(e.message||''),'error'); }
}

async function doLogout() { try { await api.auth.logout(); } catch(e) {} window.location.href = 'login.html'; }

bootstrap();
