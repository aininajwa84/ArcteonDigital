(() => {
  'use strict';
  document.querySelectorAll('[data-sep-presentation]').forEach(root => {
    const slides = [...root.querySelectorAll('[data-sep-slide]')];
    if (!slides.length) return;
    const images = [...root.querySelectorAll('[data-sep-image]')];
    const previous = [...root.querySelectorAll('[data-sep-prev]')];
    const next = [...root.querySelectorAll('[data-sep-next]')];
    const selector = root.querySelector('[data-sep-select]');
    const thumbnails = root.querySelector('.sep-thumbnails');
    const dialog = root.querySelector('dialog');
    const expand = root.querySelector('[data-sep-expand]');
    const viewer = root.querySelector('.sep-viewer');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let current = 0;

    function showSlide(index, revealThumbnail = true) {
      current = Math.max(0, Math.min(slides.length - 1, index));
      const slide = slides[current];
      const title = slide.dataset.title;
      images.forEach(image => {
        image.src = slide.href;
        // The initial Razor image has a versioned src; srcset is deliberately absent.
        image.alt = `Brochure ${current + 1}: ${title}`;
      });
      slides.forEach((item, position) => {
        if (position === current) item.setAttribute('aria-current', 'true');
        else item.removeAttribute('aria-current');
      });
      previous.forEach(button => { button.disabled = current === 0; });
      next.forEach(button => { button.disabled = current === slides.length - 1; });
      selector.value = String(current);
      root.querySelector('[data-sep-count]').textContent = `${String(current + 1).padStart(2, '0')} / ${slides.length}`;
      root.querySelector('[data-sep-title]').textContent = title;
      root.querySelector('[data-sep-dialog-count]').textContent = `${current + 1} / ${slides.length} — ${title}`;
      root.querySelector('.sep-original-slide').href = slide.href;
      const download = root.querySelector('[data-sep-download]');
      if (download) {
        download.href = slide.dataset.pdf;
        download.setAttribute('aria-label', `Download ${title} brochure as PDF`);
      }
      if (revealThumbnail) {
        thumbnails.scrollTo({
          left: Math.max(0, slide.offsetLeft - thumbnails.offsetLeft - (thumbnails.clientWidth - slide.offsetWidth) / 2),
          behavior: reducedMotion.matches ? 'instant' : 'smooth'
        });
      }
      // Fetch only the next brochure image, rather than the complete set up front.
      if (slides[current + 1]) new Image().src = slides[current + 1].href;
    }

    slides.forEach((slide, index) => slide.addEventListener('click', event => {
      if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
      event.preventDefault();
      showSlide(index);
    }));
    previous.forEach(button => button.addEventListener('click', () => showSlide(current - 1)));
    next.forEach(button => button.addEventListener('click', () => showSlide(current + 1)));
    selector.addEventListener('change', () => showSlide(Number(selector.value)));
    root.querySelectorAll('[data-sep-jump]').forEach(link => link.addEventListener('click', event => {
      event.preventDefault();
      showSlide(Number(link.dataset.sepJump));
      viewer.focus({ preventScroll: true });
    }));
    root.addEventListener('keydown', event => {
      if (event.target.matches('select, input, textarea') || event.altKey || event.ctrlKey || event.metaKey) return;
      if (event.key === 'ArrowRight' || event.key === 'ArrowLeft') {
        event.preventDefault();
        showSlide(current + (event.key === 'ArrowRight' ? 1 : -1));
      }
    });
    expand.addEventListener('click', () => {
      dialog.showModal();
      document.body.classList.add('sep-dialog-open');
    });
    root.querySelector('[data-sep-close]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('close', () => {
      document.body.classList.remove('sep-dialog-open');
      expand.focus({ preventScroll: true });
    });
    dialog.addEventListener('click', event => {
      if (event.target !== dialog) return;
      const box = dialog.getBoundingClientRect();
      if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
    });
    images.forEach(image => {
      let start;
      image.addEventListener('touchstart', event => { start = event.touches[0]; }, { passive: true });
      image.addEventListener('touchend', event => {
        if (!start) return;
        const end = event.changedTouches[0];
        const dx = end.clientX - start.clientX, dy = end.clientY - start.clientY;
        if (Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(dy) * 1.5) showSlide(current + (dx < 0 ? 1 : -1));
        start = null;
      }, { passive: true });
    });
    root.querySelector('[data-sep-controls]').hidden = false;
    expand.hidden = typeof dialog.showModal !== 'function';
    showSlide(0, false);
  });
})();
