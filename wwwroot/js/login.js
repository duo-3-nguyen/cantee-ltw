'use strict';

var authMode = 'login';
var registerRole = 'customer';

function getRedirectUrl(role) {
    if (role === 'Customer') return 'customer.html';
    if (role === 'Staff') return 'staff.html';
    return 'admin.html';
}

async function checkLoggedIn() {
    try {
        var user = await api.auth.me();
        window.location.href = getRedirectUrl(user.role);
    } catch(e) {}
}
checkLoggedIn();

function toggleAuthMode() {
    if (authMode === 'login') {
        authMode = 'register';
        document.getElementById('authLeftTitle').innerText = 'Tham gia canTee ngay!';
        document.getElementById('authLeftDesc').innerText = 'Đăng ký tài khoản để khám phá thực đơn phong phú';
        document.getElementById('authRightTitle').innerText = 'Tạo tài khoản';
        document.getElementById('roleSelectorWrap').classList.remove('ẩn');
        document.getElementById('fieldFullName').classList.remove('ẩn');
        document.getElementById('fieldEmail').classList.remove('ẩn');
        document.getElementById('authSubmitBtn').innerText = 'ĐĂNG KÝ';
        document.getElementById('authToggleLabel').textContent = 'Đã có tài khoản? ';
        document.getElementById('authToggleLink').textContent = 'Đăng nhập';
        document.getElementById('formError').classList.add('ẩn');
        if (registerRole === 'canteen') {
            document.getElementById('canteenExtraFields').classList.remove('ẩn');
        }
    } else {
        authMode = 'login';
        document.getElementById('authLeftTitle').innerText = 'Chào mừng đến với canTee!';
        document.getElementById('authLeftDesc').innerText = 'Đăng nhập để đặt món ngon mỗi ngày';
        document.getElementById('authRightTitle').innerText = 'Đăng nhập';
        document.getElementById('roleSelectorWrap').classList.add('ẩn');
        document.getElementById('fieldFullName').classList.add('ẩn');
        document.getElementById('fieldEmail').classList.add('ẩn');
        document.getElementById('canteenExtraFields').classList.add('ẩn');
        document.getElementById('authSubmitBtn').innerText = 'ĐĂNG NHẬP';
        document.getElementById('authToggleLabel').textContent = 'Chưa có tài khoản? ';
        document.getElementById('authToggleLink').textContent = 'Đăng ký';
        document.getElementById('formError').classList.add('ẩn');
    }
}

function setRegisterRole(role) {
    registerRole = role;
    var btnCust = document.getElementById('roleBtnCustomer');
    var btnCant = document.getElementById('roleBtnCanteen');
    var cFields = document.getElementById('canteenExtraFields');
    if (role === 'customer') {
        btnCust.className = 'nút-vai-trò đang-chọn';
        btnCant.className = 'nút-vai-trò';
        cFields.classList.add('ẩn');
        document.getElementById('authSubmitBtn').innerText = 'ĐĂNG KÝ';
    } else {
        btnCant.className = 'nút-vai-trò đang-chọn';
        btnCust.className = 'nút-vai-trò';
        cFields.classList.remove('ẩn');
        document.getElementById('authSubmitBtn').innerText = 'GỬI YÊU CẦU';
    }
}

function togglePassword() {
    var pwd = document.getElementById('inputPassword');
    var icon = document.getElementById('togglePwdIcon');
    if (pwd.type === 'password') {
        pwd.type = 'text';
        icon.className = 'fa-regular fa-eye-slash';
    } else {
        pwd.type = 'password';
        icon.className = 'fa-regular fa-eye';
    }
}

function setError(msg) {
    var el = document.getElementById('formError');
    el.innerText = msg;
    el.classList.remove('ẩn');
}

function clearError() {
    document.getElementById('formError').classList.add('ẩn');
}

async function handleSubmit(e) {
    e.preventDefault();
    clearError();

    var btn = document.getElementById('authSubmitBtn');
    btn.disabled = true;
    btn.innerText = 'Đang xử lý...';

    try {
        if (authMode === 'login') {
            var username = document.getElementById('inputUsername').value.trim();
            var password = document.getElementById('inputPassword').value;
            if (!username || !password) { setError('Vui lòng nhập đầy đủ thông tin.'); btn.disabled = false; updateBtnText(); return; }
            await api.auth.login(username, password);
            var user = await api.auth.me();
            showToast('Đăng nhập thành công!', 'success');
            setTimeout(function() { window.location.href = getRedirectUrl(user.role); }, 800);
        } else {
            var u = document.getElementById('inputUsername').value.trim();
            var pw = document.getElementById('inputPassword').value;
            var em = document.getElementById('inputEmail').value.trim();
            var fn = document.getElementById('inputFullName').value.trim();

            if (registerRole === 'customer') {
                if (!u || !pw || !em || !fn) { setError('Vui lòng nhập đầy đủ thông tin.'); btn.disabled = false; updateBtnText(); return; }
                await api.auth.register({ username: u, password: pw, email: em, fullName: fn });
                showToast('Đăng ký thành công! Vui lòng đăng nhập.', 'success');
                toggleAuthMode();
            } else {
                var cn = document.getElementById('inputCanteenName').value.trim();
                var ca = document.getElementById('inputCanteenAddress').value.trim();
                var cp = document.getElementById('inputCanteenPhone').value.trim();
                var ce = document.getElementById('inputCanteenEmail').value.trim();
                if (!u || !pw || !em || !fn || !cn || !ca || !cp || !ce) { setError('Vui lòng nhập đầy đủ thông tin.'); btn.disabled = false; updateBtnText(); return; }
                await api.registration.submit({
                    username: u, password: pw, email: em, fullName: fn,
                    canteenName: cn, canteenAddress: ca,
                    canteenPhoneNumber: cp, canteenEmail: ce
                });
                showToast('Yêu cầu đăng ký đã được gửi! Admin sẽ xét duyệt.', 'success');
                document.getElementById('authForm').reset();
                toggleAuthMode();
            }
        }
    } catch(err) {
        setError(err.message || 'Có lỗi xảy ra, vui lòng thử lại.');
        showToast(err.message || 'Lỗi kết nối', 'error');
    }

    btn.disabled = false;
    updateBtnText();
}

function updateBtnText() {
    var btn = document.getElementById('authSubmitBtn');
    if (authMode === 'login') btn.innerText = 'ĐĂNG NHẬP';
    else if (registerRole === 'canteen') btn.innerText = 'GỬI YÊU CẦU';
    else btn.innerText = 'ĐĂNG KÝ';
}
