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
    document.getElementById('statCards').innerHTML =
        statCard('Tổng đơn', s.totalOrders||0, 'fa-clipboard-list','nền-xanh-nhạt','color:var(--blue-main)') +
        statCard('Doanh thu', formatMoney(s.totalRevenue||0), 'fa-chart-line','nền-lục-nhạt','color:#059669') +
        statCard('Sản phẩm', s.totalProducts||0, 'fa-utensils','nền-hổ-phách-nhạt','color:#d97706') +
        statCard('Đang chờ', s.pendingOrders||0, 'fa-clock','nền-cam-nhạt','color:#ea580c');

    document.getElementById('recentOrdersBody').innerHTML = (state.recentOrders||[]).map(function(o) {
        return '<tr><td style="padding:8px;font-weight:700;color:var(--blue-main)">#'+o.id+'</td><td style="padding:8px">'+ (o.userId) +'</td><td style="padding:8px;text-align:right;font-weight:600">'+formatMoney(o.totalAmount)+'</td><td style="padding:8px;text-align:right;color:var(--text-muted)">'+formatDateTime(o.createdAt)+'</td><td style="padding:8px;text-align:center">'+ orderStatusBadge(o.status) +'</td></tr>';
    }).join('') || '<tr><td colspan="5" style="padding:16px;text-align:center;color:var(--text-muted)">Chưa có đơn hàng</td></tr>';

    document.getElementById('topProductsList').innerHTML = (state.topProducts||[]).map(function(p, i) {
        return '<div class="hàng-top-sp"><div style="display:flex;align-items:center;gap:8px"><span class="thứ-hạng">#'+(i+1)+'</span><span class="tên-top-sp">'+p.name+'</span></div><span class="số-bán">'+p.soldCount+' bán</span></div>';
    }).join('') || '<p style="font-size:0.75rem;color:var(--text-muted)">Chưa có dữ liệu</p>';
}

function statCard(label, value, icon, bgClass, colorStyle) {
    return '<div class="thẻ-chỉ-số"><div class="biểu-tượng-chỉ-số '+bgClass+'" style="'+colorStyle+'"><i class="fa-solid '+icon+'"></i></div><div><p class="nhãn-chỉ-số">'+label+'</p><p class="giá-trị-chỉ-số">'+value+'</p></div></div>';
}

function orderStatusBadge(status) {
    var m = { Pending:'nhãn nhãn-cam', Preparing:'nhãn nhãn-xanh-dương', ReadyForPickup:'nhãn nhãn-xanh-ngọc', Delivered:'nhãn nhãn-xanh-lá', Cancelled:'nhãn nhãn-đỏ' };
    var l = { Pending:'Chờ duyệt', Preparing:'Đang CB', ReadyForPickup:'Chờ giao', Delivered:'Đã giao', Cancelled:'Đã hủy' };
    return '<span class="nhãn '+(m[status]||'nhãn nhãn-xám')+'">'+(l[status]||status)+'</span>';
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
    document.getElementById('orderKanban').innerHTML = cols.map(function(col) {
        var items = state.orders.filter(function(o) { return o.status === col.status; });
        return '<div class="cột-kanban"><div class="đầu-cột '+col.color+'"><span class="tiêu-đề-cột">'+col.title+'</span><span class="đếm-cột">'+items.length+'</span></div><div class="danh-sách-cột">'+
            items.map(function(o) { return renderOrderCard(o, col.status); }).join('') +
            (items.length === 0 ? '<p style="font-size:0.75rem;color:var(--text-muted);text-align:center;padding:24px 0">Trống</p>' : '') +
        '</div></div>';
    }).join('');
}

function renderOrderCard(o, col) {
    var itemNames = (o.items||[]).map(function(i){return i.productName+' x'+i.quantity;}).join(', ');
    var canMoveLeft = col === 'Preparing' || col === 'ReadyForPickup';
    var canMoveRight = col === 'Pending' || col === 'Preparing';
    var nextStatus = col === 'Pending' ? 'Preparing' : (col === 'Preparing' ? 'ReadyForPickup' : 'Delivered');
    var prevStatus = col === 'Preparing' ? 'Pending' : (col === 'ReadyForPickup' ? 'Preparing' : null);
    return '<div class="thẻ-đơn-kanban">'+
        '<div class="đầu-thẻ-đơn"><span class="mã-đơn-kanban">#'+o.id+'</span><span class="giờ-đơn-kanban">'+formatDateTime(o.createdAt)+'</span></div>'+
        '<p class="món-đơn-kanban">'+itemNames+'</p>'+
        '<div class="dòng-tiền-kanban"><span class="tổng-tiền-kanban">'+formatMoney(o.totalAmount)+'</span><span class="trạng-thái-thanh-toán-kanban" style="color:'+(o.paymentStatus==='Paid'?'#059669':'#ef4444')+'">'+(o.paymentStatus==='Paid'?'Đã TT':'Chưa TT')+'</span></div>'+
        '<div class="nhóm-nút-kanban">'+
            (canMoveLeft ? '<button onclick="moveOrder('+o.id+',\''+prevStatus+'\')" class="nút-kanban-nhỏ mũi-tên-trái"><i class="fa-solid fa-arrow-left"></i></button>' : '')+
            (canMoveRight ? '<button onclick="moveOrder('+o.id+',\''+nextStatus+'\')" class="nút-kanban-nhỏ mũi-tên-phải"><i class="fa-solid fa-arrow-right"></i></button>' : '')+
            (col === 'ReadyForPickup' ? '<button onclick="moveOrder('+o.id+',\'Delivered\')" class="nút-kanban-nhỏ giao-hàng"><i class="fa-solid fa-check"></i> Giao</button>' : '')+
            '<button onclick="cancelOrder('+o.id+')" class="nút-kanban-nhỏ hủy-đơn"><i class="fa-solid fa-xmark"></i></button>'+
            '<button onclick="toggleOrderPayment('+o.id+',\''+o.paymentStatus+'\')" class="nút-kanban-nhỏ '+(o.paymentStatus==='Paid'?'thanh-toán-đã':'thanh-toán-chưa')+'"><i class="fa-solid '+(o.paymentStatus==='Paid'?'fa-money-bill-wave':'fa-money-bill')+'"></i></button>'+
        '</div></div>';
}

function renderOrderTable(list) {
    document.getElementById('orderTableBody').innerHTML = list.map(function(o) {
        var items = (o.items||[]).map(function(i){return i.productName+' x'+i.quantity;}).join(', ');
        return '<tr><td style="padding:12px;font-weight:700;color:var(--blue-main)">#'+o.id+'</td><td style="padding:12px">'+ (o.userId) +'</td><td style="padding:12px;color:var(--text-muted);max-width:320px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis">'+items+'</td><td style="padding:12px;text-align:right;font-weight:600">'+formatMoney(o.totalAmount)+'</td><td style="padding:12px;text-align:center">'+orderStatusBadge(o.status)+'</td><td style="padding:12px;text-align:right;color:var(--text-muted)">'+formatDateTime(o.createdAt)+'</td></tr>';
    }).join('') || '<tr><td colspan="6" style="padding:16px;text-align:center;color:var(--text-muted)">Không có đơn</td></tr>';
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
    document.getElementById('categoriesTableBody').innerHTML = state.categories.map(function(cat) {
        var count = state.products.filter(function(p) { return p.categoryId === cat.id; }).length;
        return '<tr><td style="padding:12px;font-weight:700;color:var(--text-dark)">'+cat.name+'</td><td style="padding:12px;text-align:center;color:var(--text-muted)">'+cat.displayOrder+'</td><td style="padding:12px;text-align:center;font-weight:600;color:var(--blue-main)">'+count+' món</td><td style="padding:12px;text-align:right"><div style="display:flex;align-items:center;justify-content:flex-end;gap:4px"><button onclick="openCategoryForm('+cat.id+')" class="nút-bảng-nhỏ trong-bảng sửa"><i class="fa-solid fa-pen"></i></button><button onclick="deleteCategory('+cat.id+','+count+')" class="nút-bảng-nhỏ trong-bảng xóa"><i class="fa-solid fa-trash"></i></button></div></td></tr>';
    }).join('') || '<tr><td colspan="4" style="padding:24px;text-align:center;color:var(--text-muted)">Chưa có danh mục</td></tr>';
}

function renderProductsPanel() {
    var tabs = document.getElementById('menuCategories');
    if (!state.selectedCategoryId && state.categories.length > 0) state.selectedCategoryId = state.categories[0].id;
    tabs.innerHTML = state.categories.map(function(cat) {
        var active = cat.id === state.selectedCategoryId;
        return '<button onclick="filterMenuCategory('+cat.id+')" class="nút-tab' + (active ? ' đang-chọn' : '') + '" style="white-space:nowrap">'+cat.name+'</button>';
    }).join('');

    var filtered = state.products;
    if (state.selectedCategoryId) filtered = filtered.filter(function(p) { return p.categoryId === state.selectedCategoryId; });
    document.getElementById('menuProducts').innerHTML = filtered.map(function(p) {
        return '<div class="hàng-sản-phẩm">'+
            '<div class="đầu-hàng-sp">'+
                '<div class="thông-tin-sp">'+
                    '<div class="ảnh-sp-nhỏ"><img src="'+imageUrl(p.imageUrl)+'" onerror="this.style.display=\'none\'"></div>'+
                    '<div style="flex:1">'+
                        '<div style="display:flex;align-items:center;gap:8px">'+
                            '<span class="tên-sp">'+p.name+'</span>'+
                            '<button onclick="event.stopPropagation();toggleProductStatus('+p.id+',\''+p.status+'\')" class="nhãn '+(p.status==='Available'?'nhãn-xanh-lá':'nhãn-đỏ')+'" style="font-size:0.625rem;cursor:pointer">'+(p.status==='Available'?'Còn hàng':'Hết hàng')+'</button>'+
                        '</div>'+
                        '<p class="giá-sp">'+formatMoney(p.basePriceAmount)+' &middot; '+p.soldCount+' bán</p>'+
                    '</div>'+
                '</div>'+
                '<div class="nhóm-nút-sp">'+
                    '<button onclick="openProductForm('+p.id+')" class="nút-bảng-nhỏ sửa"><i class="fa-solid fa-pen"></i></button>'+
                    '<button onclick="deleteProduct('+p.id+')" class="nút-bảng-nhỏ xóa"><i class="fa-solid fa-trash"></i></button>'+
                    '<button onclick="toggleExpanded('+p.id+')" class="nút-bảng-nhỏ mở-rộng"><i class="fa-solid fa-chevron-down"></i></button>'+
                '</div>'+
            '</div>'+
            '<div id="productMods'+p.id+'" class="khung-tùy-chọn-con ẩn">'+
                '<div id="mgList'+p.id+'" style="display:flex;flex-direction:column;gap:8px;font-size:0.75rem"></div>'+
                '<button onclick="openMgForm('+p.id+')" class="nút-xanh" style="margin-top:8px;font-size:0.625rem"><i class="fa-solid fa-plus" style="margin-right:4px"></i>Thêm nhóm</button>'+
            '</div>'+
        '</div>';
    }).join('') || '<p style="text-align:center;color:var(--text-muted);font-size:0.75rem;padding:32px 0">Chưa có sản phẩm</p>';
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
        document.getElementById('mgList'+productId).innerHTML = groups.map(function(g) {
            var modsHtml = (g.modifiers||[]).map(function(m) {
                return '<div style="display:flex;align-items:center;justify-content:space-between;padding:4px 0">'+
                    '<span style="color:var(--text-dark)">'+m.name+' '+(m.isDefault?'<span style="font-size:0.625rem;color:var(--blue-main);font-weight:700">(mặc định)</span>':'')+'</span>'+
                    '<div style="display:flex;align-items:center;gap:4px">'+
                        '<span style="color:var(--text-muted)">'+ (m.priceAmount > 0 ? '+' + formatMoney(m.priceAmount) : 'Miễn phí') +'</span>'+
                        '<button onclick="openModForm('+g.id+','+m.id+')" style="color:var(--text-muted);background:none;border:none;cursor:pointer;font-size:0.625rem"><i class="fa-solid fa-pen"></i></button>'+
                        '<button onclick="deleteModifier('+m.id+')" style="color:#f87171;background:none;border:none;cursor:pointer;font-size:0.625rem"><i class="fa-solid fa-times"></i></button>'+
                    '</div></div>';
            }).join('');
            return '<div style="background:#f8fafc;border-radius:8px;padding:8px;margin-bottom:4px">'+
                '<div style="display:flex;align-items:center;justify-content:space-between;margin-bottom:4px">'+
                    '<span style="font-weight:700;color:var(--text-muted);font-size:0.75rem">'+g.name+'</span>'+
                    '<div style="display:flex;align-items:center;gap:4px;font-size:0.625rem">'+
                        '<span style="color:var(--text-muted)">'+(g.required?'Bắt buộc':'Tùy chọn')+' &middot; Tối đa '+g.maxSelected+'</span>'+
                        '<button onclick="openMgFormForEdit('+g.id+','+productId+')" style="color:var(--text-muted);background:none;border:none;cursor:pointer"><i class="fa-solid fa-pen"></i></button>'+
                        '<button onclick="deleteModifierGroup('+g.id+')" style="color:#f87171;background:none;border:none;cursor:pointer"><i class="fa-solid fa-trash"></i></button>'+
                    '</div></div>'+
                '<div style="margin-left:12px">'+modsHtml+'</div>'+
                '<button onclick="openModForm('+g.id+')" style="margin-top:4px;font-size:0.625rem;color:var(--blue-main);font-weight:700;background:none;border:none;cursor:pointer"><i class="fa-solid fa-plus" style="margin-right:2px"></i>Thêm option</button>'+
            '</div>';
        }).join('') || '<p style="color:var(--text-muted);font-size:0.75rem">Chưa có nhóm modifier</p>';
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
    document.getElementById('operatingHoursGrid').innerHTML = days.map(function(name, i) {
        var h = (hours||[]).find(function(x) { return x.dayOfWeek === DAY_NAMES_EN[i]; }) || {};
        return '<div class="dòng-giờ-mở"><span class="ngày-giờ">'+name+'</span><input type="time" value="'+(h.openTime||'')+'" data-day="'+i+'" data-field="open"><span style="font-size:0.75rem;color:var(--text-muted)">đến</span><input type="time" value="'+(h.closeTime||'')+'" data-day="'+i+'" data-field="close"><label style="display:flex;align-items:center;gap:4px;font-size:0.75rem;color:var(--text-muted)"><input type="checkbox" data-day="'+i+'" data-field="closed" '+(h.isClosed?'checked':'')+' style="width:12px;height:12px"> Đóng</label></div>';
    }).join('');
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
