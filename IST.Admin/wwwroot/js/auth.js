window.authHelper = {
    loginWithCookie: async function (model) {
        const response = await fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(model),
            credentials: "include"
        });

        let responseDto = null;
        const contentType = response.headers.get("content-type");
        if (response.status !== 204 && contentType && contentType.includes("application/json")) {
            try {
                responseDto = await response.json(); // Ожидаем, что это будет наш ResponseDTO
            } catch (e) {
                console.error("Failed to parse JSON response body:", e);
            }
        }

        return {
            ok: response.ok,
            responseDto: responseDto // Здесь будет ваш ResponseDTO<T>
        };
    },
    logout: async function () {
        const response = await fetch("/api/auth/logout", {
            method: "POST",
            credentials: "include"
        });
        return response.ok;
    }
};