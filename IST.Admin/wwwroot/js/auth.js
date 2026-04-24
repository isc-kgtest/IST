// IST.Admin auth helper — используется LoginForm.razor для POST к /api/auth/login
window.authHelper = {
    loginWithCookie: async function (loginData) {
        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(loginData)
            });
            return await response.json();
        } catch (e) {
            return { success: false, message: 'Ошибка соединения с сервером.', statusCode: 5000 };
        }
    }
};
