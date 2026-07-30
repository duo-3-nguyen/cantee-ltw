'use strict';

var state = {
    user: null,
    canteens: [],
    currentCanteen: null,
    categories: [],
    products: [],
    cart: null,
    orders: [],
    favorites: [],
    activeCategoryId: null,
    searchQuery: '',
    orderType: 'DineIn',
    isAsap: true,
    pickupTime: null,
    note: '',
    modalQty: 1,
    orderFilter: 'all',
    customizingProductId: null,
    customizingProduct: null,
    selectedModifiers: {},
    detailCanteen: null
};

var allViews = ['viewCanteenSelect','viewMenu','viewCheckout','viewOrderHistory','viewProfile','viewChangePassword'];

function getCartTotal() {
    if (!state.cart || !state.cart.items) return { qty: 0, total: 0 };
    var qty = 0, total = 0;
    state.cart.items.forEach(function(i) { qty += i.quantity; total += i.unitPrice * i.quantity; });
    return { qty: qty, total: total };
}

function getProductById(id) {
    return (state.products || []).find(function(p) { return p.id === id; });
}

function getCanteenName(canteenId) {
    var c = state.canteens.find(function(x) { return x.id === canteenId; });
    return c ? c.name : ('Căn tin #'+canteenId);
}

function isFavorited(productId) {
    return (state.favorites || []).some(function(f) { return f.productId === productId; });
}

function showView(name) {
    allViews.forEach(function(v) { var el = document.getElementById(v); if (el) el.classList.add('ẩn'); });
    var target = document.getElementById(name);
    if (target) target.classList.remove('ẩn');
    window.scrollTo({top:0,behavior:'smooth'});
}

async function navigateTo(view) {
    if (location.hash.slice(1) !== view) history.pushState(null, '', '#' + view);
    var needsCanteen = ['menu','checkout'];
    if (!state.currentCanteen && needsCanteen.indexOf(view) !== -1) {
        showToast('Vui lòng chọn căn tin trước!', 'info');
        view = 'canteen-select';
    }

    var pill = document.getElementById('headerCanteenPill');
    var prompt = document.getElementById('headerCanteenPrompt');
    var wrapper = document.getElementById('headerCanteenWrapper');
    var statusEl = document.getElementById('headerCanteenStatus');

    if (view === 'canteen-select') {
        pill.classList.add('ẩn'); pill.style.display = 'none';
        prompt.classList.remove('ẩn');
        statusEl.classList.add('ẩn');
        showView('viewCanteenSelect');
        renderCanteenSelect();
    } else {
        var hideCanteenPill = (view === 'order-history');
        if (hideCanteenPill) {
            wrapper.classList.add('ẩn');
        } else {
            wrapper.classList.remove('ẩn');
            if (state.currentCanteen) {
                prompt.classList.add('ẩn');
                pill.classList.remove('ẩn'); pill.style.display = 'flex';
                document.getElementById('headerCanteenName').innerText = state.currentCanteen.name;
                updateHeaderCanteenStatus();
            } else {
                pill.classList.add('ẩn'); pill.style.display = 'none';
                prompt.classList.remove('ẩn');
                statusEl.classList.add('ẩn');
            }
        }

        if (view === 'menu') {
            await loadMenuData();
            showView('viewMenu');
            renderCategories();
            renderProducts();
            renderCart();
        } else if (view === 'checkout') {
            showView('viewCheckout');
            generatePickupOptions();
            renderCheckout();
        } else if (view === 'order-history') {
            if (!state.orders || state.orders.length === 0) await loadOrders();
            showView('viewOrderHistory');
            renderOrderHistory();
        } else if (view === 'profile') {
            showView('viewProfile');
            renderProfile();
        } else if (view === 'change-password') {
            showView('viewChangePassword');
            document.getElementById('pwdError').classList.add('ẩn');
        }
    }
}

function selectCanteenView() { navigateTo('canteen-select'); }

function updateHeaderCanteenStatus() {
    var el = document.getElementById('headerCanteenStatus');
    if (!state.currentCanteen) { el.classList.add('ẩn'); return; }
    var open = isCanteenOpen(state.currentCanteen);
    var hours = getTodayHours(state.currentCanteen);
    el.classList.remove('ẩn');
    el.className = 'trạng-thái-căn-tin nhãn ' + (open ? 'nhãn-xanh-lá' : 'nhãn-đỏ');
    el.innerHTML = (open ? '<i class="fa-solid fa-circle" style="font-size:0.375rem;margin-right:4px"></i>Đang mở' : '<i class="fa-solid fa-circle" style="font-size:0.375rem;margin-right:4px"></i>Đã đóng') + ' &middot; ' + hours;
}

function renderCanteenSelect() {
    var q = (document.getElementById('welcomeSearch')||{}).value || '';
    var list = state.canteens.filter(function(c) {
        return !q || c.name.toLowerCase().indexOf(q.toLowerCase()) !== -1 || c.address.toLowerCase().indexOf(q.toLowerCase()) !== -1;
    });
    var grid = document.getElementById('canteenGrid');
    if (list.length === 0) {
        grid.innerHTML = '<div class="chỗ-trống"><i class="fa-solid fa-store-slash" style="font-size:1.875rem;display:block;margin-bottom:8px"></i><p style="font-size:0.875rem">Không tìm thấy căn tin</p></div>';
        return;
    }
    grid.innerHTML = list.map(function(c) {
        var isSel = state.currentCanteen && state.currentCanteen.id === c.id;
        var open = isCanteenOpen(c);
        return '<div class="thẻ-căn-tin' + (isSel ? ' đang-chọn-căn-tin' : '') + '">'+
            '<div class="bên-trái-thẻ" onclick="selectCanteen('+c.id+')">'+
                '<img src="'+imageUrl(c.imageUrl)+'" onerror="this.src=\''+CONFIG.PLACEHOLDER_IMAGE+'\'">'+
                '<div>'+
                    '<div class="nhóm-nhãn">'+
                        '<h3 class="tên-căn-tin">'+c.name+'</h3>'+
                        '<span class="nhãn '+(ENUM_LABELS.canteenStatus[c.status]||{}).cls+'">'+(ENUM_LABELS.canteenStatus[c.status]||{}).label+'</span>'+
                        (open ? '<span class="nhãn nhãn-xanh-lá">Đang mở</span>' : '<span class="nhãn nhãn-đỏ">Đóng</span>')+
                    '</div>'+
                    '<p class="địa-chỉ-căn-tin">'+c.address+'</p>'+
                    '<span class="thông-tin-phụ">'+c.phoneNumber+' &middot; '+getTodayHours(c)+'</span>'+
                '</div>'+
            '</div>'+
            '<div class="nhóm-nút-phải">'+
                '<button onclick="event.stopPropagation();openCanteenDetail('+c.id+')" class="nút-xanh"><i class="fa-solid fa-circle-info"></i><span class="ẩn" style="display:none">Chi tiết</span></button>'+
                '<button onclick="selectCanteen('+c.id+')" class="nút-chọn-căn-tin' + (isSel ? ' đã-chọn' : '') + '">'+(isSel ? 'Đang chọn' : 'Chọn')+'</button>'+
            '</div>'+
        '</div>';
    }).join('');
}

async function selectCanteen(id) {
    var c = state.canteens.find(function(x) { return x.id === id; });
    if (!c) return;
    state.currentCanteen = c;
    state.cart = null;
    state.categories = [];
    state.products = [];
    state.activeCategoryId = null;
    localStorage.setItem('lastCanteenId', c.id);
    try {
        state.cart = await api.cart.get(c.id);
        state.favorites = await api.favorites.list();
        renderCanteenSelect();
        navigateTo('menu');
    } catch(e) {
        showToast('Lỗi tải dữ liệu căn tin: ' + (e.message || ''), 'error');
    }
}

function toggleCanteenDropdown() {
    var dd = document.getElementById('canteenDropdown');
    if (dd.classList.contains('ẩn')) {
        document.getElementById('canteenSearchInput').value = '';
        renderCanteenDropdown(state.canteens);
        dd.classList.remove('ẩn');
        setTimeout(function(){ document.getElementById('canteenSearchInput').focus(); }, 100);
    } else {
        dd.classList.add('ẩn');
    }
}

function filterCanteenDropdown() {
    var q = document.getElementById('canteenSearchInput').value.toLowerCase();
    var filtered = q ? state.canteens.filter(function(c) { return c.name.toLowerCase().indexOf(q) !== -1 || c.address.toLowerCase().indexOf(q) !== -1; }) : state.canteens;
    renderCanteenDropdown(filtered);
}

function renderCanteenDropdown(list) {
    var el = document.getElementById('canteenDropdownList');
    el.innerHTML = list.map(function(c) {
        var isSel = state.currentCanteen && state.currentCanteen.id === c.id;
        return '<div class="dòng-căn-tin-dd' + (isSel ? ' đang-chọn' : '') + '">'+
            '<div class="bấm-chọn" onclick="selectCanteen('+c.id+');document.getElementById(\'canteenDropdown\').classList.add(\'ẩn\')">'+
                '<div style="width:40px;height:40px;border-radius:8px;background:#f1f5f9;color:var(--navy);display:flex;align-items:center;justify-content:center;font-weight:700;font-size:0.875rem;flex-shrink:0"><i class="fa-solid fa-store"></i></div>'+
                '<div style="flex:1;min-width:0;margin-left:12px">'+
                    '<h4 class="tên-căn-tin-dd">'+c.name+'</h4>'+
                    '<p class="địa-chỉ-căn-tin-dd">'+c.address+'</p>'+
                '</div>'+
            '</div>'+
            '<button onclick="event.stopPropagation();openCanteenDetail('+c.id+');document.getElementById(\'canteenDropdown\').classList.add(\'ẩn\')" class="liên-kết" style="font-size:0.75rem;flex-shrink:0;padding:0 8px">Chi tiết</button>'+
        '</div>';
    }).join('');
    if (list.length === 0) el.innerHTML = '<div style="text-align:center;color:var(--text-muted);font-size:0.75rem;padding:24px 0">Không tìm thấy căn tin</div>';
}

function openCanteenDetail(id) {
    var c = state.canteens.find(function(x) { return x.id === id; });
    if (!c) return;
    state.detailCanteen = c;
    document.getElementById('detailModalTitle').innerText = c.name;
    document.getElementById('detailModalAddress').innerText = c.address;
    document.getElementById('detailModalHours').innerText = 'Giờ: ' + getTodayHours(c);
    document.getElementById('detailModalPhone').innerText = 'SĐT: ' + c.phoneNumber;
    document.getElementById('detailModalImage').src = imageUrl(c.imageUrl);
    var today = new Date().getDay();
    var days = ['Chủ nhật','Thứ Hai','Thứ Ba','Thứ Tư','Thứ Năm','Thứ Sáu','Thứ Bảy'];
    document.getElementById('detailModalWeeklyHours').innerHTML = days.map(function(name, i) {
        var h = (c.operatingHours||[]).find(function(x) { return normalizeDayOfWeek(x.dayOfWeek) === i; }) || {};
        var isToday = i === today;
        var line = h.isClosed ? 'Đóng cửa' : ((formatTimeOnly(h.openTime)||'--:--') + ' - ' + (formatTimeOnly(h.closeTime)||'--:--'));
        return '<div class="dòng-lịch' + (isToday ? ' hôm-nay' : '') + '"><span>'+name+'</span><span>'+line+'</span></div>';
    }).join('');
    document.getElementById('canteenDetailModal').classList.remove('ẩn');
}

function closeCanteenDetail() { document.getElementById('canteenDetailModal').classList.add('ẩn'); state.detailCanteen = null; }

function selectCanteenFromDetail() {
    if (state.detailCanteen) { selectCanteen(state.detailCanteen.id); closeCanteenDetail(); }
}

async function loadMenuData() {
    if (!state.currentCanteen) return;
    try {
        state.categories = await api.categories.list(state.currentCanteen.id);
        state.cart = await api.cart.get(state.currentCanteen.id);
        state.favorites = await api.favorites.list();
        loadProducts();
    } catch(e) { showToast('Lỗi tải menu: ' + (e.message || ''), 'error'); }
}

async function loadProducts() {
    if (!state.currentCanteen) return;
    try {
        var params = { canteenId: state.currentCanteen.id };
        if (state.activeCategoryId) params.categoryId = state.activeCategoryId;
        if (state.searchQuery) params.search = state.searchQuery;
        state.products = await api.products.list(params);
        renderProducts();
    } catch(e) { showToast('Lỗi tải sản phẩm: ' + (e.message || ''), 'error'); }
}

function renderCategories() {
    var tabs = document.getElementById('categoryTabs');
    var cats = state.categories || [];
    var isAll = !state.activeCategoryId;
    tabs.innerHTML = '<button onclick="setCategory(null)" class="nút-tab' + (isAll ? ' đang-chọn' : '') + '">Tất cả</button>' +
        cats.map(function(cat) {
            var isActive = cat.id === state.activeCategoryId;
            return '<button onclick="setCategory('+cat.id+')" class="nút-tab' + (isActive ? ' đang-chọn' : '') + '">'+cat.name+'</button>';
        }).join('');
}

async function setCategory(id) {
    state.activeCategoryId = id;
    renderCategories();
    await loadProducts();
}

function handleSearch(val) {
    state.searchQuery = val;
    loadProducts();
}

async function toggleFavorite(productId) {
    try {
        if (isFavorited(productId)) {
            await api.favorites.remove(productId);
            state.favorites = state.favorites.filter(function(f) { return f.productId !== productId; });
            showToast('Đã bỏ yêu thích', 'info');
        } else {
            await api.favorites.add(productId);
            state.favorites = await api.favorites.list();
            showToast('Đã thêm vào yêu thích', 'success');
        }
        renderProducts();
    } catch(e) { showToast(e.message || 'Lỗi', 'error'); }
}

function renderProducts() {
    var grid = document.getElementById('productGrid');
    var found = state.categories.find(function(c) { return c.id === state.activeCategoryId; });
    document.getElementById('categoryTitle').innerText = found ? found.name : 'Tất cả món';
    document.getElementById('productCount').innerText = (state.products||[]).length + ' món';

    if ((state.products||[]).length === 0) {
        grid.innerHTML = '<div class="chỗ-trống" style="grid-column:1/-1"><i class="fa-solid fa-utensils" style="font-size:2.25rem;margin-bottom:12px;opacity:0.3"></i><p style="font-size:0.875rem;font-weight:500">Không tìm thấy món ăn</p></div>';
        return;
    }

    grid.innerHTML = state.products.map(function(p) {
        var fav = isFavorited(p.id);
        var outOfStock = p.status === 'OutOfStock';
        return '<div class="thẻ-món-ăn">'+
            '<div>'+
                '<div class="ảnh-món">'+
                    '<img src="'+imageUrl(p.imageUrl)+'" alt="'+p.name+'" onerror="this.src=\''+CONFIG.PLACEHOLDER_IMAGE+'\'">'+
                    (outOfStock ? '<div class="lớp-hết-hàng"><span>Hết hàng</span></div>' : '')+
                    '<button onclick="event.stopPropagation();toggleFavorite('+p.id+')" class="nút-yêu-thích">'+
                        '<i class="fa-'+(fav ? 'solid' : 'regular')+' fa-heart" style="color:'+(fav ? '#ef4444' : 'var(--text-muted)')+';font-size:0.875rem"></i>'+
                    '</button>'+
                '</div>'+
                '<div class="chi-tiết-món">'+
                    '<h3 class="tên-món" title="'+p.name+'">'+p.name+'</h3>'+
                    '<p class="mô-tả-món">'+(p.description || '')+'</p>'+
                    '<span class="chỉ-số-phụ">'+p.soldCount+' đã bán &middot; <i class="fa-regular fa-heart" style="color:#f87171"></i> '+p.favoriteCount+'</span>'+
                '</div>'+
            '</div>'+
            '<div class="chân-món">'+
                '<span class="giá-món">'+formatMoney(p.basePriceAmount)+'</span>'+
                (outOfStock ? '<span class="tạm-hết">Tạm hết</span>' :
                 '<button onclick="openCustomizeModal('+p.id+')" class="nút-thêm"><i class="fa-solid fa-plus"></i></button>'
                )+
            '</div>'+
        '</div>';
    }).join('');
}

async function refreshCart() {
    if (!state.currentCanteen) return;
    try { state.cart = await api.cart.get(state.currentCanteen.id); renderCart(); renderProducts(); } catch(e) { showToast('Lỗi giỏ hàng: ' + (e.message || ''), 'error'); }
}

function renderCart() {
    var list = document.getElementById('cartItems');
    var badge = document.getElementById('cartBadge');
    var totalEl = document.getElementById('cartTotal');
    var btn = document.getElementById('checkoutBtn');
    var items = (state.cart && state.cart.items) ? state.cart.items.slice().sort(function(a, b) { return a.id - b.id; }) : [];
    var info = getCartTotal();
    badge.innerText = info.qty + ' món';
    totalEl.innerText = formatMoney(info.total);
    btn.disabled = info.qty === 0;

    if (items.length === 0) {
        list.innerHTML = '<div class="chỗ-trống"><i class="fa-solid fa-basket-shopping" style="font-size:1.875rem;margin-bottom:8px;color:#cbd5e1"></i><p style="font-size:0.75rem;font-weight:500">Giỏ hàng trống</p></div>';
        return;
    }

    list.innerHTML = items.map(function(item) {
        var mods = parseModifiersJson(item.selectedModifiersJson);
        var modLines = '';
        mods.forEach(function(mg) {
            if (mg.modifiers && mg.modifiers.length) {
                modLines += '<p class="tùy-chọn-giỏ">'+mg.groupName+': '+mg.modifiers.map(function(m){ return m.name + (m.priceAmount > 0 ? ' (+'+formatMoney(m.priceAmount)+')' : ''); }).join(', ')+'</p>';
            }
        });
        return '<div class="dòng-món-giỏ">'+
            '<div style="flex:1;min-width:0">'+
                '<h4 class="tên-món-giỏ">'+item.productName+'</h4>'+
                modLines +
                (item.note ? '<p class="ghi-chú-giỏ">Ghi chú: '+item.note+'</p>' : '')+
                '<div class="dòng-giá-số-lượng">'+
                    '<span class="giá-đơn-vị">'+formatMoney(item.unitPrice)+'</span>'+
                    '<div class="bộ-chọn-số-lượng-nhỏ">'+
                        '<button onclick="updateCartQtyByItemId('+item.id+','+(item.quantity-1)+')" class="nút-giảm">-</button>'+
                        '<span class="số-lượng">'+item.quantity+'</span>'+
                        '<button onclick="updateCartQtyByItemId('+item.id+','+(item.quantity+1)+')" class="nút-tăng">+</button>'+
                    '</div>'+
                '</div>'+
            '</div>'+
            '<button onclick="removeCartItem('+item.id+')" class="nút-xóa-món"><i class="fa-solid fa-trash"></i></button>'+
        '</div>';
    }).join('');
}

async function updateCartQtyByItemId(itemId, newQty) {
    if (!state.currentCanteen) return;
    try {
        if (newQty <= 0) { await api.cart.removeItem(state.currentCanteen.id, itemId); }
        else { await api.cart.updateItem(state.currentCanteen.id, itemId, { quantity: newQty }); }
        await refreshCart();
    } catch(e) { showToast(e.message || 'Lỗi cập nhật giỏ', 'error'); }
}

async function updateCartQty(productId, newQty) {
    if (!state.currentCanteen) return;
    var existing = (state.cart && state.cart.items) ? state.cart.items.find(function(i) { return i.productId === productId; }) : null;
    try {
        if (!existing || newQty <= 0) { if (existing) await api.cart.removeItem(state.currentCanteen.id, existing.id); }
        else { await api.cart.updateItem(state.currentCanteen.id, existing.id, { quantity: newQty }); }
        await refreshCart();
    } catch(e) { showToast(e.message || 'Lỗi', 'error'); }
}

async function removeCartItem(itemId) {
    if (!state.currentCanteen) return;
    try { await api.cart.removeItem(state.currentCanteen.id, itemId); await refreshCart(); } catch(e) { showToast(e.message || 'Lỗi', 'error'); }
}

async function openCustomizeModal(productId) {
    try {
        var detail = await api.products.get(productId);
        state.customizingProductId = productId;
        state.customizingProduct = detail;
        state.selectedModifiers = {};
        state.modalQty = 1;
        document.getElementById('modalQtyValue').innerText = '1';
        document.getElementById('modalProductName').innerText = detail.name;
        document.getElementById('modalProductPrice').innerText = formatMoney(detail.basePriceAmount);
        document.getElementById('modalProductImage').src = imageUrl(detail.imageUrl);
        document.getElementById('modalNote').value = '';

        var groupsHtml = '';
        var groups = detail.modifierGroups || [];
        groups.forEach(function(mg) {
            if (mg.status !== 'Available') return;
            state.selectedModifiers[mg.id] = [];
            var mods = (mg.modifiers || []).filter(function(m) { return m.status === 'Available'; });

            groupsHtml += '<div class="nhóm-tùy-chọn">'+
                '<label class="tên-nhóm">'+mg.name+(mg.required ? ' <span class="bắt-buộc">*</span>' : '')+'</label>';

            if (mg.maxSelected <= 1) {
                groupsHtml += '<div>';
                mods.forEach(function(m) {
                    groupsHtml += '<label class="dòng-tùy-chọn">'+
                        '<input type="radio" name="modgrp_'+mg.id+'" value="'+m.id+'" onchange="selectModifier('+mg.id+','+m.id+',true,'+m.priceAmount+',\''+m.name.replace(/'/g,"\\'")+'\')" '+(m.isDefault ? 'checked' : '')+'>'+
                        '<span class="tên-tùy-chọn">'+m.name+'</span>'+
                        (m.priceAmount > 0 ? '<span class="giá-tùy-chọn">+'+formatMoney(m.priceAmount)+'</span>' : '')+
                    '</label>';
                });
                groupsHtml += '</div>';
            } else {
                groupsHtml += '<div>';
                mods.forEach(function(m) {
                    groupsHtml += '<label class="dòng-tùy-chọn">'+
                        '<input type="checkbox" name="modgrp_'+mg.id+'" value="'+m.id+'" onchange="selectModifier('+mg.id+','+m.id+',false,'+m.priceAmount+',\''+m.name.replace(/'/g,"\\'")+'\')" '+(m.isDefault ? 'checked' : '')+'>'+
                        '<span class="tên-tùy-chọn">'+m.name+'</span>'+
                        (m.priceAmount > 0 ? '<span class="giá-tùy-chọn">+'+formatMoney(m.priceAmount)+'</span>' : '')+
                    '</label>';
                });
                groupsHtml += '</div>';
            }
            groupsHtml += '</div>';
        });

        document.getElementById('modalModifierGroups').innerHTML = groupsHtml || '<p style="font-size:0.75rem;color:var(--text-muted)">Món này không có tùy chọn thêm.</p>';

        var detailGroups = detail.modifierGroups || [];
        detailGroups.forEach(function(mg) {
            var checked = document.querySelectorAll('input[name="modgrp_'+mg.id+'"]:checked');
            state.selectedModifiers[mg.id] = [];
            checked.forEach(function(cb) {
                var nameEl = cb.parentElement.querySelector('.tên-tùy-chọn');
                state.selectedModifiers[mg.id].push({ id: parseInt(cb.value), name: nameEl ? nameEl.innerText : '', priceAmount: 0 });
            });
        });

        document.getElementById('productModal').classList.remove('ẩn');
    } catch(e) { showToast('Lỗi tải chi tiết món: ' + (e.message || ''), 'error'); }
}

function selectModifier(groupId, modId, isRadio, priceAmount, name) {
    if (isRadio) {
        state.selectedModifiers[groupId] = [{ id: modId, name: name, priceAmount: priceAmount || 0 }];
    } else {
        if (!state.selectedModifiers[groupId]) state.selectedModifiers[groupId] = [];
        var cbs = document.querySelectorAll('input[name="modgrp_'+groupId+'"]:checked');
        state.selectedModifiers[groupId] = [];
        cbs.forEach(function(cb) {
            var nameEl = cb.parentElement.querySelector('.tên-tùy-chọn');
            var n = nameEl ? nameEl.innerText : '';
            state.selectedModifiers[groupId].push({ id: parseInt(cb.value), name: n, priceAmount: 0 });
        });
    }
}

function closeProductModal() { document.getElementById('productModal').classList.add('ẩn'); state.customizingProductId = null; state.customizingProduct = null; state.modalQty = 1; }

function modalDecreaseQty() {
    if (state.modalQty <= 1) return;
    state.modalQty--;
    document.getElementById('modalQtyValue').innerText = state.modalQty;
}

function modalIncreaseQty() {
    if (state.modalQty >= 99) return;
    state.modalQty++;
    document.getElementById('modalQtyValue').innerText = state.modalQty;
}

async function confirmAddToCart() {
    if (!state.currentCanteen || !state.customizingProductId) return;
    var product = state.customizingProduct || getProductById(state.customizingProductId);
    if (!product) return;
    var noteVal = document.getElementById('modalNote').value.trim();
    var modsPayload = [];
    var detailGroups = (state.customizingProduct && state.customizingProduct.modifierGroups) || [];
    detailGroups.forEach(function(mg) {
        var sel = state.selectedModifiers[mg.id];
        if (sel && sel.length > 0) { modsPayload.push({ groupId: mg.id, groupName: mg.name, modifiers: sel }); }
    });
    var body = {
        productId: state.customizingProductId,
        quantity: state.modalQty,
        note: noteVal || undefined,
        selectedModifiersJson: modsPayload.length > 0 ? JSON.stringify(modsPayload) : undefined
    };
    try {
        await api.cart.addItem(state.currentCanteen.id, body);
        closeProductModal();
        await refreshCart();
        showToast('Đã thêm vào giỏ!', 'success');
    } catch(e) { showToast(e.message || 'Lỗi thêm vào giỏ', 'error'); }
}

function generatePickupOptions() {
    var sel = document.getElementById('pickupSelect');
    sel.innerHTML = '<option value="asap">Lấy ngay (sớm nhất)</option>';
    sel.value = 'asap';
    state.isAsap = true;
    state.pickupTime = null;
    var c = state.currentCanteen;
    if (!c) return;
    if (!c.operatingHours) {
        api.operatingHours.list(c.id).then(function(list) { if (c && list) c.operatingHours = list; generatePickupOptions(); }).catch(function(){});
        return;
    }
    var today = new Date().getDay();
    var hours = c.operatingHours.find(function(h) { return normalizeDayOfWeek(h.dayOfWeek) === today; });
    if (!hours || hours.isClosed) return;
    var openStr = formatTimeOnly(hours.openTime), closeStr = formatTimeOnly(hours.closeTime);
    if (!openStr || !closeStr) return;
    var now = new Date();
    var nowMinutes = now.getHours() * 60 + now.getMinutes();
    var openParts = openStr.split(':'), closeParts = closeStr.split(':');
    var openMinutes = parseInt(openParts[0]) * 60 + parseInt(openParts[1]);
    var closeMinutes = parseInt(closeParts[0]) * 60 + parseInt(closeParts[1]);
    var startMinutes = Math.max(nowMinutes, openMinutes);
    startMinutes = Math.ceil(startMinutes / 30) * 30;
    for (var m = startMinutes; m <= closeMinutes - 30; m += 30) {
        var hh = String(Math.floor(m / 60)).padStart(2, '0');
        var mm = String(m % 60).padStart(2, '0');
        sel.innerHTML += '<option value="' + hh + ':' + mm + '">' + hh + ':' + mm + '</option>';
    }
}

function setOrderType(type) {
    state.orderType = type;
    document.getElementById('btnDineIn').className = type === 'DineIn'
        ? 'nút-hình-thức đang-chọn dine-in'
        : 'nút-hình-thức';
    document.getElementById('btnTakeaway').className = type === 'TakeAway'
        ? 'nút-hình-thức đang-chọn mang-về'
        : 'nút-hình-thức';
}

function onPickupChange() {
    var val = document.getElementById('pickupSelect').value;
    state.isAsap = (val === 'asap');
    state.pickupTime = state.isAsap ? null : val;
}

function renderCheckout() {
    var items = (state.cart && state.cart.items) ? state.cart.items.slice().sort(function(a, b) { return a.id - b.id; }) : [];
    var totalQty = 0, subtotal = 0;
    items.forEach(function(i) { totalQty += i.quantity; subtotal += i.unitPrice * i.quantity; });
    document.getElementById('checkoutItemCount').innerText = totalQty;
    document.getElementById('summaryCount').innerText = 'Tạm tính ('+totalQty+' món)';
    document.getElementById('summarySubtotal').innerText = formatMoney(subtotal);
    document.getElementById('summaryTotal').innerText = formatMoney(subtotal);
    document.getElementById('placeOrderBtn').disabled = items.length === 0;
    var container = document.getElementById('checkoutItems');
    if (items.length === 0) {
        container.innerHTML = '<div class="chỗ-trống"><i class="fa-solid fa-basket-shopping" style="font-size:1.875rem;margin-bottom:8px;opacity:0.3"></i><p style="font-size:0.75rem">Chưa có món nào</p></div>';
        return;
    }
    container.innerHTML = items.map(function(item) {
        var mods = parseModifiersJson(item.selectedModifiersJson);
        var modLines = '';
        mods.forEach(function(mg) {
            if (mg.modifiers && mg.modifiers.length) {
                modLines += '<p class="tùy-chọn-ck">'+mg.groupName+': '+mg.modifiers.map(function(m){ return m.name; }).join(', ')+'</p>';
            }
        });
        return '<div class="dòng-món-checkout">'+
            '<div style="display:flex;align-items:flex-start;gap:12px">'+
                '<span class="số-lượng-x">'+item.quantity+'X</span>'+
                '<div>'+
                    '<h3 class="tên-món-ck">'+item.productName+'</h3>'+
                    modLines +
                    (item.note ? '<p class="ghi-chú-ck">GC: '+item.note+'</p>' : '')+
                '</div>'+
            '</div>'+
            '<span class="giá-món-ck">'+formatMoney(item.unitPrice * item.quantity)+'</span>'+
        '</div>';
    }).join('');
}

async function placeOrder() {
    var items = (state.cart && state.cart.items) ? state.cart.items.slice().sort(function(a, b) { return a.id - b.id; }) : [];
    if (items.length === 0) { showToast('Giỏ hàng trống!', 'error'); return; }
    var btn = document.getElementById('placeOrderBtn');
    btn.disabled = true; btn.innerText = 'Đang đặt...';
    try {
        var body = {
            canteenId: state.currentCanteen.id,
            orderType: state.orderType,
            isAsap: state.isAsap,
            pickupTime: state.isAsap ? null : (state.pickupTime || null),
            note: document.getElementById('orderNote').value.trim() || undefined
        };
        var order = await api.orders.create(body);
        state.cart = null;
        document.getElementById('successOrderId').innerText = '#' + order.id;
        document.getElementById('successModal').classList.remove('ẩn');
    } catch(e) { showToast(e.message || 'Lỗi tạo đơn', 'error'); }
    btn.disabled = false; btn.innerText = 'Đặt món';
}

function closeSuccessModal() { document.getElementById('successModal').classList.add('ẩn'); navigateTo('order-history'); }

async function loadOrders() {
    try { state.orders = await api.orders.list(); } catch(e) { showToast('Lỗi tải lịch sử đơn: ' + (e.message || ''), 'error'); }
}

function setOrderFilter(filter) {
    state.orderFilter = filter;
    ['All','Active','Completed'].forEach(function(f) {
        var el = document.getElementById('filter'+f);
        if (!el) return;
        var match = f.toLowerCase() === filter;
        el.className = 'nút-tab-lọc' + (match ? ' đang-chọn' : '');
    });
    renderOrderHistory();
}

function renderOrderHistory() {
    var list = document.getElementById('orderList');
    var orders = state.orders || [];
    var activeStatuses = ['Pending','Preparing','ReadyForPickup'];
    var completedStatuses = ['Delivered','Cancelled'];
    if (state.orderFilter === 'active') orders = orders.filter(function(o) { return activeStatuses.indexOf(o.status) !== -1; });
    else if (state.orderFilter === 'completed') orders = orders.filter(function(o) { return completedStatuses.indexOf(o.status) !== -1; });
    if (orders.length === 0) {
        list.innerHTML = '<div class="thẻ-trắng" style="text-align:center;padding:40px"><i class="fa-solid fa-clock-rotate-left" style="font-size:2.25rem;margin-bottom:12px;opacity:0.3"></i><p style="font-size:0.875rem">Chưa có đơn hàng nào</p></div>';
        return;
    }
    list.innerHTML = orders.map(function(o) {
        var st = ENUM_LABELS.orderStatus[o.status] || {label:o.status,cls:'nhãn nhãn-xám'};
        var pm = ENUM_LABELS.paymentStatus[o.paymentStatus] || {label:o.paymentStatus,cls:'nhãn nhãn-xám'};
        return '<div class="thẻ-đơn-hàng">'+
            '<div class="đầu-đơn">'+
                '<div style="display:flex;align-items:center;gap:8px">'+
                    '<span class="mã-đơn">#'+o.id+'</span>'+
                    '<span class="nhãn '+st.cls+'">'+st.label+'</span>'+
                    '<span class="nhãn '+pm.cls+'">'+pm.label+'</span>'+
                '</div>'+
                '<span class="nhãn-loại-đơn">'+(ENUM_LABELS.orderType[o.orderType]||{}).label+'</span>'+
            '</div>'+
            '<div class="thời-gian-đơn">'+formatDateTime(o.createdAt)+' &middot; '+getCanteenName(o.canteenId)+'</div>'+
            '<div style="padding:12px 0">'+
                (o.items||[]).map(function(i) {
                    var mods = parseModifiersJson(i.selectedModifiersJson);
                    var desc = '';
                    mods.forEach(function(mg) { if (mg.modifiers && mg.modifiers.length) desc += mg.modifiers.map(function(m){ return m.name; }).join(', '); });
                    return '<div class="dòng-món-ls">'+
                        '<span class="sl-đơn">'+i.quantity+'x</span>'+
                        '<span class="tên-món-ls">'+i.productName+'</span>'+
                        (desc ? '<span class="tùy-chọn-ls">('+desc+')</span>' : '')+
                        '<span class="giá-ls">'+formatMoney(i.subTotal)+'</span>'+
                    '</div>';
                }).join('')+
            '</div>'+
            '<div class="chân-đơn">'+
                '<div><span style="font-size:0.75rem;color:var(--text-muted);display:block">Tổng thanh toán</span><span class="tổng-đơn">'+formatMoney(o.totalAmount)+'</span></div>'+
                (o.note ? '<span style="font-size:0.75rem;color:var(--text-muted);font-style:italic">GC: '+o.note+'</span>' : '')+
            '</div>'+
        '</div>';
    }).join('');
}

function renderProfile() {
    document.getElementById('profUsername').innerText = state.user.username;
    document.getElementById('inputFullName').value = state.user.fullName || '';
    document.getElementById('inputEmail').value = state.user.email || '';
    document.getElementById('profRole').innerText = 'Khách hàng';
    document.getElementById('profileError').classList.add('ẩn');
}

async function saveProfile(e) {
    e.preventDefault();
    var errEl = document.getElementById('profileError');
    errEl.classList.add('ẩn');
    var fullName = document.getElementById('inputFullName').value.trim();
    var email = document.getElementById('inputEmail').value.trim();
    if (!fullName || !email) { errEl.innerText = 'Vui lòng nhập đầy đủ.'; errEl.classList.remove('ẩn'); return; }
    if (fullName === state.user.fullName && email === state.user.email) { showToast('Không có thông tin nào thay đổi.', 'info'); return; }
    try {
        var updated = await api.auth.updateProfile({ fullName: fullName, email: email });
        state.user = updated;
        updateUserUI();
        showToast('Cập nhật thông tin thành công!', 'success');
    } catch(e) {
        errEl.innerText = e.message || 'Lỗi cập nhật thông tin.';
        errEl.classList.remove('ẩn');
    }
}

async function changePassword(e) {
    e.preventDefault();
    var oldPwd = document.getElementById('oldPassword').value;
    var newPwd = document.getElementById('newPassword').value;
    var errEl = document.getElementById('pwdError');
    errEl.classList.add('ẩn');
    if (!oldPwd || !newPwd) { errEl.innerText = 'Vui lòng nhập đầy đủ.'; errEl.classList.remove('ẩn'); return; }
    if (newPwd.length < 6) { errEl.innerText = 'Mật khẩu mới tối thiểu 6 ký tự.'; errEl.classList.remove('ẩn'); return; }
    try {
        await api.auth.changePassword(oldPwd, newPwd, true);
        showToast('Đổi mật khẩu thành công! Vui lòng đăng nhập lại.', 'success');
        setTimeout(function(){ window.location.href = 'login.html'; }, 1500);
    } catch(e) {
        errEl.innerText = e.message || 'Lỗi đổi mật khẩu.';
        errEl.classList.remove('ẩn');
    }
}

function toggleUserDropdown() {
    var dd = document.getElementById('userDropdown');
    dd.classList.toggle('ẩn');
}
function closeUserDropdown() { document.getElementById('userDropdown').classList.add('ẩn'); }

document.addEventListener('click', function(e) {
    var ud = document.getElementById('userDropdown');
    var btn = e.target.closest('button');
    if (ud && !ud.classList.contains('ẩn') && (!btn || btn.onclick.toString().indexOf('toggleUserDropdown') === -1)) ud.classList.add('ẩn');
    var cd = document.getElementById('canteenDropdown');
    if (cd && !cd.classList.contains('ẩn')) {
        var pill = document.getElementById('headerCanteenPill');
        if (!cd.contains(e.target) && (!pill || !pill.contains(e.target))) cd.classList.add('ẩn');
    }
});

async function doLogout() {
    try { await api.auth.logout(); } catch(e) {}
    window.location.href = 'login.html';
}

async function bootstrap() {
    try {
        var user = await api.auth.me();
        state.user = user;
        if (user.role !== 'Customer') {
            showToast('Trang này dành cho khách hàng. Vui lòng đăng nhập với tài khoản Customer.', 'error');
            setTimeout(function(){ window.location.href = 'login.html'; }, 2000);
            return;
        }
        updateUserUI();
        var canteens = await api.canteens.list('Active');
        state.canteens = canteens || [];
        window.addEventListener('popstate', function() { navigateTo(location.hash.slice(1) || 'canteen-select'); });
        var lastId = parseInt(localStorage.getItem('lastCanteenId') || '');
        if (lastId && canteens && canteens.some(function(c) { return c.id === lastId; })) {
            await selectCanteen(lastId);
        } else {
            navigateTo('canteen-select');
        }
    } catch(e) {
        if (e.status === 401) { window.location.href = 'login.html'; }
        else { showToast('Lỗi kết nối: ' + (e.message || 'Không thể kết nối server'), 'error'); setTimeout(function(){ window.location.href = 'login.html'; }, 2000); }
    }
}

function updateUserUI() {
    if (!state.user) return;
    document.getElementById('headerUserName').innerText = state.user.fullName || state.user.username;
    document.getElementById('dropdownUserName').innerText = state.user.fullName || state.user.username;
    document.getElementById('dropdownUserRole').innerText = 'Khách hàng';
    document.getElementById('headerUserAvatar').src = 'https://ui-avatars.com/api/?name=' + encodeURIComponent(state.user.fullName || state.user.username) + '&background=003b7a&color=fff&size=80';
}

bootstrap();
