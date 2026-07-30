'use strict';

const CONFIG = {
    API_BASE: '',
    PLACEHOLDER_IMAGE: 'data:image/svg+xml,' + encodeURIComponent('<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" fill="#e2e8f0"><rect width="400" height="300"/><text x="50%" y="50%" dominant-baseline="middle" text-anchor="middle" fill="#94a3b8" font-size="16" font-family="sans-serif">No Image</text></svg>')
};

const ENUM_LABELS = {
    canteenStatus: {
        Active:     { label: 'Đang hoạt động', cls: 'nhãn nhãn-xanh-lá' },
        Suspended:  { label: 'Tạm ngưng',       cls: 'nhãn nhãn-đỏ' }
    },
    orderStatus: {
        Pending:        { label: 'Chờ xác nhận',   cls: 'nhãn nhãn-cam' },
        Preparing:      { label: 'Đang chuẩn bị',   cls: 'nhãn nhãn-xanh-dương' },
        ReadyForPickup: { label: 'Sẵn sàng nhận',   cls: 'nhãn nhãn-xanh-ngọc' },
        Delivered:      { label: 'Đã giao',         cls: 'nhãn nhãn-xanh-lá' },
        Cancelled:      { label: 'Đã hủy',          cls: 'nhãn nhãn-đỏ' }
    },
    orderType: {
        DineIn:  { label: 'Ăn tại chỗ', icon: 'fa-utensils' },
        TakeAway:{ label: 'Mang về',     icon: 'fa-box-archive' }
    },
    paymentStatus: {
        Unpaid: { label: 'Chưa TT', cls: 'nhãn nhãn-xám' },
        Paid:   { label: 'Đã TT',   cls: 'nhãn nhãn-xanh-lá' }
    },
    stockStatus: {
        Available:  { label: 'Còn hàng',    cls: 'nhãn nhãn-xanh-lá' },
        OutOfStock: { label: 'Hết hàng',    cls: 'nhãn nhãn-đỏ' }
    },
    weekDay: ['Chủ nhật','Thứ Hai','Thứ Ba','Thứ Tư','Thứ Năm','Thứ Sáu','Thứ Bảy']
};

function formatMoney(amount) {
    if (amount == null) return '0đ';
    return Number(amount).toLocaleString('vi-VN') + 'đ';
}

function formatDateTime(isoStr) {
    if (!isoStr) return '';
    const d = new Date(isoStr);
    const day = String(d.getDate()).padStart(2,'0');
    const month = String(d.getMonth()+1).padStart(2,'0');
    const hour = String(d.getHours()).padStart(2,'0');
    const min = String(d.getMinutes()).padStart(2,'0');
    return `${day}/${month} ${hour}:${min}`;
}

function formatTimeOnly(timeStr) {
    if (!timeStr) return '';
    return timeStr.substring(0, 5);
}

function parseModifiersJson(jsonStr) {
    if (!jsonStr) return [];
    try { return JSON.parse(jsonStr); } catch(e) { return []; }
}

function showToast(msg, type) {
    type = type || 'info';
    var toast = document.getElementById('toast');
    if (!toast) return;
    var icon = document.getElementById('toastIcon');
    var msgEl = document.getElementById('toastMessage');
    if (icon) {
        var icons = { info:'fa-circle-info', success:'fa-circle-check', error:'fa-circle-exclamation' };
        icon.className = 'fa-solid ' + (icons[type] || icons.info);
    }
    if (msgEl) msgEl.innerText = msg;
    toast.classList.add('hiện');
    clearTimeout(toast._timeout);
    toast._timeout = setTimeout(function() {
        toast.classList.remove('hiện');
    }, 3000);
}

function imageUrl(path) {
    if (!path) return CONFIG.PLACEHOLDER_IMAGE;
    if (path.startsWith('http')) return path;
    return CONFIG.API_BASE + path;
}

var DAY_NAMES_EN = ['Sunday','Monday','Tuesday','Wednesday','Thursday','Friday','Saturday'];

function normalizeDayOfWeek(val) {
    if (typeof val === 'number') return val;
    var idx = DAY_NAMES_EN.indexOf(val);
    if (idx >= 0) return idx;
    var idx2 = ENUM_LABELS.weekDay.indexOf(val);
    if (idx2 >= 0) return idx2;
    return parseInt(val, 10);
}

function getTodayHours(canteen) {
    var today = new Date().getDay();
    var hours = (canteen.operatingHours || []).find(function(h) {
        return normalizeDayOfWeek(h.dayOfWeek) === today;
    });
    if (!hours || hours.isClosed) return 'Đóng cửa';
    return (formatTimeOnly(hours.openTime) || '07:00') + ' - ' + (formatTimeOnly(hours.closeTime) || '17:00');
}

function isCanteenOpen(canteen) {
    var today = new Date().getDay();
    var hours = (canteen.operatingHours || []).find(function(h) {
        return normalizeDayOfWeek(h.dayOfWeek) === today;
    });
    if (!hours || hours.isClosed) return false;
    var now = new Date();
    var nowStr = String(now.getHours()).padStart(2,'0') + ':' + String(now.getMinutes()).padStart(2,'0');
    if (hours.openTime && hours.closeTime) {
        var open = formatTimeOnly(hours.openTime);
        var close = formatTimeOnly(hours.closeTime);
        return nowStr >= open && nowStr < close;
    }
    return true;
}
