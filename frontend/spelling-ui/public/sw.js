self.addEventListener('push', event => {
  const data = event.data?.text() || 'Review your spelling lesson!';
  event.waitUntil(
    self.registration.showNotification('Spelling App', {
      body: data,
    })
  );
});
