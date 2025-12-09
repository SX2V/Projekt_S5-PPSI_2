(function () {
    window.onload = function () {
        const ui = window.ui;
        if (!ui) return;

        ui.getConfigs().requestInterceptor = (req) => {
            req.headers['Accept-Language'] = 'pl-PL'; 
            return req;
        };
    };
})();
