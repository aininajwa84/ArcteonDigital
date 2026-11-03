(function () {
  const buttons = document.querySelectorAll('[data-product-filter]');
  const cards = document.querySelectorAll('[data-product-group]');
  buttons.forEach(button => button.addEventListener('click', () => {
    let count = 0;
    buttons.forEach(item => {
      const active = item === button;
      item.classList.toggle('active', active);
      item.setAttribute('aria-pressed', String(active));
    });
    cards.forEach(card => {
      card.hidden = button.dataset.productFilter !== 'all' && card.dataset.productGroup !== button.dataset.productFilter;
      if (!card.hidden) count++;
    });
    document.getElementById('product-filter-status').textContent = `${count} ${count === 1 ? 'solution' : 'solutions'} to explore`;
  }));
})();
