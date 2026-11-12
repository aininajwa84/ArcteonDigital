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
  const interestField = document.getElementById('application-interest');
  const interestOptions = document.getElementById('application-interest-options');
  const generalInterest = document.getElementById('application-general-interest');
  const internshipDates = document.getElementById('internship-dates');
  const startDate = document.getElementById('application-date');
  const endDate = document.getElementById('application-end-date');

  function updateInternshipDates() {
    const isInternship = !interestField.disabled && interestField.value === 'Internship';
    internshipDates.hidden = internshipDates.disabled = !isInternship;
    endDate.min = startDate.value;
  }

  interestField.addEventListener('change', updateInternshipDates);
  startDate.addEventListener('input', updateInternshipDates);
  const resume = document.getElementById('application-resume');
  const supportingDocuments = document.getElementById('application-supporting-documents');
  const maxFileBytes = 5 * 1024 * 1024;
  form.addEventListener('reset', () => {
    resume.setCustomValidity('');
    supportingDocuments.setCustomValidity('');
    queueMicrotask(updateInternshipDates);
  });
  modal.addEventListener('show.bs.modal', event => {
    const role = event.relatedTarget?.dataset.role || 'General Interest';
    const interest = event.relatedTarget?.dataset.interest || 'General Interest';
    form.reset();
    document.getElementById('careerApplicationTitle').textContent = role;
    const context = document.getElementById('application-role-context');
    if (context) context.textContent = event.relatedTarget?.dataset.description || 'Tell us about your skills, interests and the opportunity you have in mind.';
    const roleField = document.getElementById('application-role');
    roleField.value = roleField.defaultValue = role;
    const isGeneralInterest = interest === 'General Interest';
    interestOptions.hidden = interestField.disabled = isGeneralInterest;
    generalInterest.hidden = !isGeneralInterest;
    for (const option of interestField.options) {
      option.defaultSelected = option.value === interest;
    }
    interestField.value = isGeneralInterest ? 'Internship' : interest;
    updateInternshipDates();
    form.querySelectorAll('.loading, .error-message, .sent-message').forEach(el => el.classList.remove('d-block'));
  });
  function validateAttachments() {
    const resumeSize = resume.files[0]?.size ?? 0;
    const documents = Array.from(supportingDocuments.files);
    resume.setCustomValidity(resumeSize > maxFileBytes ? 'Please choose a résumé no larger than 5 MB.' : '');

    let error = '';
    if (documents.length > 3) {
      error = 'Please choose up to three supporting documents.';
    } else if (documents.some(file => file.size === 0 || file.size > maxFileBytes)) {
      error = 'Each supporting document must be non-empty and 5 MB or smaller.';
    } else if (documents.reduce((total, file) => total + file.size, resumeSize) > 15 * 1024 * 1024) {
      error = 'Attachments must be 15 MB or smaller in total.';
    }
    supportingDocuments.setCustomValidity(error);
  }
  resume.addEventListener('change', validateAttachments);
  supportingDocuments.addEventListener('change', validateAttachments);
})();
