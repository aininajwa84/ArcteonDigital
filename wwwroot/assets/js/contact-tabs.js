(function () {
  const selector = document.getElementById('enquiry-type');
  if (!selector) return;
  const project = document.getElementById('contact-project');
  const job = document.getElementById('contact-job');

  function showEnquiry(kind, scroll) {
    const isJob = kind === 'job';
    selector.value = isJob ? 'job' : 'project';
    project.hidden = isJob;
    job.hidden = !isJob;
    // Only the selected form participates in browser validation and submission.
    [project, job].forEach(panel => {
      panel.querySelectorAll('input, textarea, button').forEach(field => {
        field.disabled = panel.hidden;
      });
    });
    if (scroll) selector.closest('.enquiry-selector').scrollIntoView({ block: 'start' });
  }
  function openLinkedEnquiry() {
    if (location.hash === '#contact-job') showEnquiry('job', true);
    else if (location.hash === '#contact-project' || location.hash === '#contact') showEnquiry('project', true);
  }
  selector.addEventListener('change', () => {
    showEnquiry(selector.value, false);
    history.replaceState(null, '', selector.value === 'job' ? '#contact-job' : '#contact-project');
  });
  showEnquiry(location.hash === '#contact-job' ? 'job' : 'project', false);
  window.addEventListener('load', openLinkedEnquiry);
  window.addEventListener('hashchange', openLinkedEnquiry);
  document.addEventListener('click', event => {
    const link = event.target.closest('a[href]');
    if (!link) return;
    const url = new URL(link.href, location.href);
    if (url.origin !== location.origin || url.pathname !== location.pathname ||
        !['#contact-job', '#contact-project', '#contact'].includes(url.hash)) return;
    event.preventDefault();
    history.replaceState(null, '', url.hash);
    openLinkedEnquiry();
  });
})();
