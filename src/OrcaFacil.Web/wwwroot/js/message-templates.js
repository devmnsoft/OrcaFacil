(() => {
  const safeInit = (name, initialize) => {
    try { initialize(); } catch (error) { console.error(`[OrcaFácil:${name}]`, error); }
  };

  safeInit('message-templates', () => {
    const root = document.querySelector('[data-template-workspace]');
    if (!root) return;

    const form = root.querySelector('[data-template-form]');
    if (!form) return;

    const channel = form.querySelector('[data-template-channel]');
    const subject = form.querySelector('[data-template-subject]');
    const body = form.querySelector('[data-template-body]');
    const allowed = new Set([...form.querySelectorAll('[data-variable]')].map((item) => item.dataset.variable));
    const samples = { ClienteNome: 'Ana', EmpresaNome: 'Horizonte Serviços', NumeroOrcamento: 'ORC-1042', ValorTotal: 'R$ 1.850,00', Validade: '30/08/2026', LinkPublico: 'orcafacil.com.br/p/seguro', NomeUsuario: 'Marina', TelefoneEmpresa: '(11) 99999-0000' };

    const render = (value) => value.replace(/\{([A-Za-z]+)\}/g, (match, key) => samples[key] || match);
    const messageText = () => `${subject.required && subject.value ? `${subject.value}\n\n` : ''}${body.value}`.trim();
    const refresh = () => {
      const isEmail = channel.value === 'Email';
      form.querySelector('[data-subject-field]').classList.toggle('is-required', isEmail);
      subject.required = isEmail;
      root.querySelector('[data-preview-title]').textContent = isEmail ? 'E-mail' : channel.value === 'WhatsApp' ? 'WhatsApp' : 'Mensagem';
      const subjectPreview = root.querySelector('[data-preview-subject]');
      subjectPreview.hidden = !isEmail;
      subjectPreview.textContent = render(subject.value) || 'Assunto da mensagem';
      root.querySelector('[data-preview-body]').textContent = render(body.value) || 'Comece a escrever para visualizar a mensagem.';
      root.querySelector('[data-body-count]').textContent = body.value.length;
      const variables = [...`${subject.value} ${body.value}`.matchAll(/\{([^{}]*)\}/g)].map((match) => match[1]);
      const bracesMatch = (`${subject.value}${body.value}`.match(/\{/g) || []).length === (`${subject.value}${body.value}`.match(/\}/g) || []).length;
      root.querySelector('[data-preview-warning]').hidden = bracesMatch && variables.every((item) => allowed.has(item));
      root.querySelector('[data-open-whatsapp]').href = `https://wa.me/?text=${encodeURIComponent(messageText())}`;
    };

    form.addEventListener('input', refresh);
    form.querySelectorAll('[data-variable]').forEach((button) => button.addEventListener('click', () => {
      const token = `{${button.dataset.variable}}`;
      const start = body.selectionStart;
      body.setRangeText(token, start, body.selectionEnd, 'end');
      body.focus();
      refresh();
    }));
    root.querySelector('[data-copy-template]').addEventListener('click', async () => {
      const feedback = root.querySelector('[data-share-feedback]');
      if (!messageText()) { feedback.textContent = 'Escreva a mensagem antes de copiar.'; return; }
      try {
        await navigator.clipboard.writeText(messageText());
        feedback.textContent = 'Mensagem copiada. Revise e confirme o envio no canal escolhido.';
      } catch {
        feedback.textContent = 'O navegador não permitiu a cópia. Selecione o texto no preview para copiar.';
      }
    });
    refresh();
  });
})();
