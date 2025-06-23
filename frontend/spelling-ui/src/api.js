const API_BASE = '/api';

export async function uploadLessonImage(file) {
  const form = new FormData();
  form.append('file', file);
  const res = await fetch(`${API_BASE}/lessons/upload`, {
    method: 'POST',
    body: form,
  });
  return res.json();
}

export async function saveLesson(lesson) {
  const res = await fetch(`${API_BASE}/lessons`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(lesson),
  });
  return res.json();
}

export async function getLesson(id) {
  const res = await fetch(`${API_BASE}/lessons/${id}`);
  return res.json();
}
