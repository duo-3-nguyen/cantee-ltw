'use strict';

var api = {};

api._fetch = function(url, options) {
    options = options || {};
    options.credentials = 'include';
    if (!options.headers) options.headers = {};
    if (!options.headers['Content-Type'] && !(options.body instanceof FormData)) {
        options.headers['Content-Type'] = 'application/json';
    }
    return fetch(CONFIG.API_BASE + url, options).then(function(res) {
        var ct = res.headers.get('content-type') || '';
        return res.text().then(function(text) {
            var data;
            if (ct.indexOf('application/json') !== -1) {
                try { data = JSON.parse(text); } catch(e) { data = text; }
            } else {
                data = text;
            }
            if (!res.ok) {
                var msg;
                if (typeof data === 'string') {
                    msg = data;
                } else if (data.errors) {
                    var errs = [];
                    Object.keys(data.errors).forEach(function(k) { errs.push(k + ': ' + data.errors[k].join(', ')); });
                    msg = errs.join('; ') || data.title || JSON.stringify(data);
                } else {
                    msg = data.message || data.detail || data.title || data.error || JSON.stringify(data);
                }
                throw { status: res.status, message: msg, data: data };
            }
            return data;
        });
    });
};

api._post = function(url, body) {
    return api._fetch(url, { method: 'POST', body: body ? JSON.stringify(body) : undefined });
};

api._put = function(url, body) {
    return api._fetch(url, { method: 'PUT', body: body ? JSON.stringify(body) : undefined });
};

api._patch = function(url, body) {
    return api._fetch(url, { method: 'PATCH', body: body ? JSON.stringify(body) : undefined });
};

api._delete = function(url) {
    return api._fetch(url, { method: 'DELETE' });
};

api.auth = {
    me: function() {
        return api._fetch('/api/auth/me');
    },
    login: function(username, password) {
        return api._post('/api/auth/login', { username: username, password: password });
    },
    register: function(data) {
        return api._post('/api/auth/register', data);
    },
    logout: function() {
        return api._post('/api/auth/logout');
    },
    changePassword: function(oldPassword, newPassword, logoutAllDevices) {
        return api._post('/api/auth/change-password', {
            oldPassword: oldPassword,
            newPassword: newPassword,
            logoutAllDevices: logoutAllDevices !== false
        });
    },
    updateProfile: function(data) {
        return api._put('/api/auth/profile', data);
    }
};

api.canteens = {
    list: function(status) {
        var url = '/api/canteens';
        if (status) url += '?status=' + encodeURIComponent(status);
        return api._fetch(url);
    },
    get: function(id) {
        return api._fetch('/api/canteens/' + id);
    }
};

api.categories = {
    list: function(canteenId) {
        return api._fetch('/api/canteens/' + canteenId + '/categories');
    }
};

api.products = {
    list: function(params) {
        var qs = [];
        if (params) {
            if (params.canteenId) qs.push('canteenId=' + params.canteenId);
            if (params.categoryId) qs.push('categoryId=' + params.categoryId);
            if (params.search) qs.push('search=' + encodeURIComponent(params.search));
            if (params.status) qs.push('status=' + encodeURIComponent(params.status));
        }
        var url = '/api/products' + (qs.length ? '?' + qs.join('&') : '');
        return api._fetch(url);
    },
    get: function(id) {
        return api._fetch('/api/products/' + id);
    }
};

api.cart = {
    get: function(canteenId) {
        return api._fetch('/api/canteens/' + canteenId + '/cart');
    },
    addItem: function(canteenId, data) {
        return api._post('/api/canteens/' + canteenId + '/cart/items', data);
    },
    updateItem: function(canteenId, itemId, data) {
        return api._put('/api/canteens/' + canteenId + '/cart/items/' + itemId, data);
    },
    removeItem: function(canteenId, itemId) {
        return api._delete('/api/canteens/' + canteenId + '/cart/items/' + itemId);
    },
    clear: function(canteenId) {
        return api._delete('/api/canteens/' + canteenId + '/cart');
    }
};

api.orders = {
    create: function(data) {
        return api._post('/api/orders', data);
    },
    list: function() {
        return api._fetch('/api/orders');
    },
    get: function(id) {
        return api._fetch('/api/orders/' + id);
    }
};

api.favorites = {
    list: function() {
        return api._fetch('/api/favorites');
    },
    add: function(productId) {
        return api._post('/api/favorites/' + productId);
    },
    remove: function(productId) {
        return api._delete('/api/favorites/' + productId);
    }
};

api.registration = {
    submit: function(data) {
        return api._post('/api/registration-requests', data);
    }
};

api.canteens.listWithStaff = function(staffId) {
    return api._fetch('/api/canteens?staffId=' + staffId);
};
api.canteens.update = function(id, data) { return api._put('/api/canteens/' + id, data); };
api.canteens.uploadImage = function(id, formData) { return api._fetch('/api/canteens/' + id + '/image', { method: 'POST', body: formData }); };
api.canteens.updateStatus = function(id, data) { return api._patch('/api/canteens/' + id + '/status', data); };

api.categories.create = function(canteenId, data) { return api._post('/api/canteens/' + canteenId + '/categories', data); };
api.categories.update = function(id, data) { return api._put('/api/categories/' + id, data); };
api.categories.delete = function(id) { return api._delete('/api/categories/' + id); };

api.products.create = function(canteenId, formData) { return api._fetch('/api/canteens/' + canteenId + '/products', { method: 'POST', body: formData }); };
api.products.update = function(id, data) { return api._put('/api/products/' + id, data); };
api.products.updateStatus = function(id, data) { return api._patch('/api/products/' + id + '/status', data); };
api.products.delete = function(id) { return api._delete('/api/products/' + id); };
api.products.uploadImage = function(id, formData) { return api._fetch('/api/products/' + id + '/image', { method: 'POST', body: formData }); };

api.modifierGroups = {
    list: function(productId) { return api._fetch('/api/products/' + productId + '/modifier-groups'); },
    create: function(productId, data) { return api._post('/api/products/' + productId + '/modifier-groups', data); },
    update: function(id, data) { return api._put('/api/modifier-groups/' + id, data); },
    updateStatus: function(id, data) { return api._patch('/api/modifier-groups/' + id + '/status', data); },
    delete: function(id) { return api._delete('/api/modifier-groups/' + id); }
};

api.modifiers = {
    list: function(groupId) { return api._fetch('/api/modifier-groups/' + groupId + '/modifiers'); },
    create: function(groupId, data) { return api._post('/api/modifier-groups/' + groupId + '/modifiers', data); },
    update: function(id, data) { return api._put('/api/modifiers/' + id, data); },
    updateStatus: function(id, data) { return api._patch('/api/modifiers/' + id + '/status', data); },
    delete: function(id) { return api._delete('/api/modifiers/' + id); }
};

api.operatingHours = {
    list: function(canteenId) { return api._fetch('/api/canteens/' + canteenId + '/operating-hours'); },
    update: function(canteenId, data) { return api._put('/api/canteens/' + canteenId + '/operating-hours', data); }
};

api.orders.listByCanteen = function(canteenId, params) {
    var qs = [];
    if (params) {
        if (params.status) qs.push('status=' + encodeURIComponent(params.status));
        if (params.date) qs.push('date=' + encodeURIComponent(params.date));
    }
    return api._fetch('/api/canteens/' + canteenId + '/orders' + (qs.length ? '?' + qs.join('&') : ''));
};
api.orders.updateStatus = function(id, data) { return api._patch('/api/orders/' + id + '/status', data); };
api.orders.updatePayment = function(id, data) { return api._patch('/api/orders/' + id + '/payment', data); };

api.dashboard = {
    revenue: function(params) {
        var qs = [];
        if (params && params.from) qs.push('from=' + encodeURIComponent(params.from));
        if (params && params.to) qs.push('to=' + encodeURIComponent(params.to));
        return api._fetch('/api/dashboard/revenue' + (qs.length ? '?' + qs.join('&') : ''));
    },
    canteenStats: function(canteenId) { return api._fetch('/api/dashboard/canteens/' + canteenId + '/stats'); },
    recentOrders: function(canteenId) { return api._fetch('/api/dashboard/canteens/' + canteenId + '/orders/recent'); },
    topProducts: function(canteenId) { return api._fetch('/api/dashboard/canteens/' + canteenId + '/products/top'); }
};

api.users = {
    list: function() { return api._fetch('/api/users'); },
    update: function(id, data) { return api._put('/api/users/' + id, data); },
    updateStatus: function(id, data) { return api._patch('/api/users/' + id + '/status', data); }
};

api.registrationRequests = {
    list: function(status) {
        var url = '/api/registration-requests';
        if (status) url += '?status=' + encodeURIComponent(status);
        return api._fetch(url);
    },
    approve: function(id) { return api._post('/api/registration-requests/' + id + '/approve'); },
    reject: function(id) { return api._post('/api/registration-requests/' + id + '/reject'); }
};
