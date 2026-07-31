const drawer = document.querySelector('[data-account-switcher]');
const opener = document.querySelector('[data-account-switcher-open]');

if (drawer && opener) {
    const list = drawer.querySelector('[data-account-list]');
    const error = drawer.querySelector('[data-account-error]');
    const search = drawer.querySelector('[data-account-search]');
    const searchWrap = drawer.querySelector('[data-account-search-wrap]');
    let accounts = [];

    const close = () => {
        drawer.hidden = true;
        opener.focus();
    };

    const render = () => {
        const term = search.value.trim().toLocaleLowerCase('pt-BR');
        const visible = accounts.filter(account => account.name.toLocaleLowerCase('pt-BR').includes(term));
        list.replaceChildren(...visible.map(account => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'of-account-choice';
            button.dataset.accountId = account.accountId;
            button.disabled = account.isCurrent || account.status !== 'Active';
            button.innerHTML = `<span class="of-avatar" aria-hidden="true">${account.name.slice(0, 1).toUpperCase()}</span>
                <span><strong></strong><small></small><em></em></span><span class="of-status-dot"></span>`;
            button.querySelector('strong').textContent = account.name;
            button.querySelector('small').textContent = `${account.role} · plano ${account.planCode}`;
            button.querySelector('em').textContent = account.isCurrent ? 'Conta atual' :
                account.status === 'Active' ? 'Disponível' : 'Indisponível';
            return button;
        }));
        if (!visible.length) list.innerHTML = '<p>Nenhuma conta encontrada.</p>';
    };

    const load = async () => {
        list.setAttribute('aria-busy', 'true');
        error.hidden = true;
        try {
            const response = await fetch('/Internal/Accounts', { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error('Não foi possível carregar suas contas.');
            accounts = (await response.json()).accounts;
            searchWrap.hidden = accounts.length < 5;
            render();
        } catch (exception) {
            error.textContent = exception.message;
            error.hidden = false;
            list.replaceChildren();
        } finally {
            list.setAttribute('aria-busy', 'false');
        }
    };

    opener.addEventListener('click', () => {
        drawer.hidden = false;
        drawer.querySelector('[data-account-switcher-close]').focus();
        load();
    });
    drawer.querySelector('[data-account-switcher-close]').addEventListener('click', close);
    search.addEventListener('input', render);
    drawer.addEventListener('click', async event => {
        if (event.target === drawer) return close();
        const choice = event.target.closest('[data-account-id]');
        if (!choice) return;
        choice.disabled = true;
        choice.setAttribute('aria-busy', 'true');
        error.hidden = true;
        try {
            const token = drawer.querySelector('input[name="__RequestVerificationToken"]').value;
            const response = await fetch('/Internal/Accounts/Switch', {
                method: 'POST', headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token },
                body: JSON.stringify({ accountId: choice.dataset.accountId })
            });
            const payload = await response.json();
            if (!response.ok) throw new Error(payload.message || 'Não foi possível trocar a conta.');
            window.location.assign(payload.redirectUrl);
        } catch (exception) {
            choice.disabled = false;
            choice.removeAttribute('aria-busy');
            error.textContent = exception.message;
            error.hidden = false;
        }
    });
    document.addEventListener('keydown', event => { if (event.key === 'Escape' && !drawer.hidden) close(); });
}
