(() => {
    'use strict';
    const form = document.querySelector('[data-pipeline-filters]');
    if (!form) return;
    const stage = form.querySelector('[data-pipeline-stage]');
    const client = form.querySelector('[data-pipeline-client]');
    const apply = () => {
        const selectedStage = stage?.value ?? '';
        const term = (client?.value ?? '').trim().toLocaleLowerCase('pt-BR');
        document.querySelectorAll('[data-pipeline-column]').forEach(column => {
            const stageMatches = !selectedStage || column.dataset.pipelineColumn === selectedStage;
            let visibleCards = 0;
            column.querySelectorAll('[data-client]').forEach(card => {
                const visible = !term || (card.dataset.client ?? '').includes(term);
                card.hidden = !visible;
                if (visible) visibleCards += 1;
            });
            column.hidden = !stageMatches || (Boolean(term) && visibleCards === 0);
        });
    };
    stage?.addEventListener('change', apply);
    client?.addEventListener('input', apply);
})();
