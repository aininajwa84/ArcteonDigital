(function () {
  const filters = document.querySelectorAll('[data-career-filter]');
  const cards = document.querySelectorAll('[data-career-kind]');
  const search = document.getElementById('career-search');
  let activeKind = 'all';
  function updateRoles() {
    const query = (search?.value || '').trim().toLowerCase();
    let count = 0;
    cards.forEach(card => {
      card.hidden = (activeKind !== 'all' && card.dataset.careerKind !== activeKind) || !card.textContent.toLowerCase().includes(query);
      if (!card.hidden) count++;
    });
    const status = document.getElementById('career-filter-status');
    if (status) status.textContent = `Showing ${count} sample ${count === 1 ? 'opportunity' : 'opportunities'}`;
    const empty = document.getElementById('career-empty');
    if (empty) empty.hidden = count !== 0;
    filters.forEach(filter => {
      const selected = filter.dataset.careerFilter === activeKind;
      filter.classList.toggle('active', selected);
      filter.setAttribute('aria-pressed', String(selected));
    });
  }
  filters.forEach(button => button.addEventListener('click', () => {
    activeKind = button.dataset.careerFilter;
    updateRoles();
  }));
  search?.addEventListener('input', updateRoles);
  document.getElementById('career-reset')?.addEventListener('click', () => {
    activeKind = 'all';
    search.value = '';
    updateRoles();
    search.focus();
  });
  const modal = document.getElementById('careerApplication');
  if (!modal) return;
  const form = document.getElementById('career-application-form');
  modal.addEventListener('show.bs.modal', event => {
    const role = event.relatedTarget?.dataset.role || 'General Interest';
    const interest = event.relatedTarget?.dataset.interest || 'General Interest';
    form.reset();
    document.getElementById('careerApplicationTitle').textContent = role;
    const context = document.getElementById('application-role-context');
    if (context) context.textContent = event.relatedTarget?.dataset.description || 'Tell us about your skills, interests and the opportunity you have in mind.';
    const roleField = document.getElementById('application-role');
    roleField.value = roleField.defaultValue = role;
    const interestField = document.getElementById('application-interest');
    Array.from(interestField.options).forEach(option => option.defaultSelected = option.value === interest);
    interestField.value = interest;
    form.querySelectorAll('.loading, .error-message, .sent-message').forEach(el => el.classList.remove('d-block'));
    document.getElementById('application-resume').setCustomValidity('');
  });
  document.getElementById('application-resume').addEventListener('change', event => {
    const file = event.target.files[0];
    event.target.setCustomValidity(file && file.size > 5 * 1024 * 1024 ? 'Please choose a résumé no larger than 5 MB.' : '');
  });
})();
