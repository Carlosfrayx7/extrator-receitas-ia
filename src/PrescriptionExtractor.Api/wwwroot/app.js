const $ = (id) => document.getElementById(id);
const STATUS = { valid: ['Válida', 'ok'], needs_review: ['Revisar', 'warn'], invalid: ['Inválida', 'bad'] };
const CONF = { high: 'alta', medium: 'média', low: 'baixa' };

fetch('/health').then((r) => r.json()).then((h) => { $('key-warning').hidden = h.credentialsConfigured; });

fetch('/samples').then((r) => r.json()).then((samples) => {
  for (const s of samples) {
    const li = document.createElement('li');
    const btn = document.createElement('button');
    btn.textContent = s.name;
    btn.onclick = async () => {
      const blob = await (await fetch(s.url)).blob();
      extract(new File([blob], s.name, { type: blob.type }));
    };
    li.append(btn);
    $('samples').append(li);
  }
});

$('dropzone').onclick = () => $('file-input').click();
$('file-input').onchange = (e) => e.target.files[0] && extract(e.target.files[0]);
$('dropzone').ondragover = (e) => { e.preventDefault(); $('dropzone').classList.add('over'); };
$('dropzone').ondragleave = () => $('dropzone').classList.remove('over');
$('dropzone').ondrop = (e) => {
  e.preventDefault();
  $('dropzone').classList.remove('over');
  if (e.dataTransfer.files[0]) extract(e.dataTransfer.files[0]);
};

function showPreview(file) {
  const url = URL.createObjectURL(file);
  $('preview').innerHTML = file.type === 'application/pdf'
    ? `<object data="${url}" type="application/pdf"></object>`
    : `<img src="${url}" alt="Prévia da receita">`;
}

async function extract(file) {
  showPreview(file);
  $('status').className = 'loading';
  $('status').textContent = 'Extraindo...';
  $('fields').innerHTML = '';
  $('issues').innerHTML = '';
  $('usage').textContent = '';

  const form = new FormData();
  form.append('file', file);
  form.append('model', $('model').value);
  const response = await fetch('/extractions', { method: 'POST', body: form });
  const body = await response.json();
  if (!response.ok) {
    $('status').className = 'bad';
    $('status').textContent = body.error;
    return;
  }
  render(body);
}

function row(label, field, flagged) {
  const tr = document.createElement('tr');
  if (flagged) tr.className = 'review';
  const value = field.value ?? '—';
  tr.innerHTML = `<th>${label}</th><td></td><td><span class="conf ${field.confidence}">${CONF[field.confidence]}</span></td>`;
  tr.children[1].textContent = value;
  return tr;
}

function render(r) {
  const [label, css] = STATUS[r.status];
  $('status').className = css;
  $('status').textContent = `Situação: ${label}`;

  const flagged = new Set(r.issues.map((i) => i.field));
  const p = r.prescription;
  const rows = [
    ['Médico', p.prescriber.name, 'prescriber.name'],
    ['CRM', p.prescriber.crmNumber, 'prescriber.crmNumber'],
    ['UF', p.prescriber.crmState, 'prescriber.crmState'],
    ['Paciente', p.patientName, 'patientName'],
    ['Data', p.issueDate, 'issueDate'],
  ];
  p.items.forEach((it, i) => {
    rows.push([`Medicamento ${i + 1}`, it.medication, `items[${i}].medication`]);
    rows.push(['Concentração', it.strength, `items[${i}].strength`]);
    rows.push(['Forma', it.form, `items[${i}].form`]);
    rows.push(['Posologia', it.dosage, `items[${i}].dosage`]);
    rows.push(['Duração', it.duration, `items[${i}].duration`]);
    rows.push(['Quantidade', it.quantity, `items[${i}].quantity`]);
  });
  for (const [label2, field, path] of rows) $('fields').append(row(label2, field, flagged.has(path)));

  for (const issue of r.issues) {
    const li = document.createElement('li');
    li.className = issue.severity;
    li.textContent = issue.message;
    $('issues').append(li);
  }
  const model = r.model.includes('haiku') ? 'Haiku 5.5' : 'Opus 5.5';
  $('usage').textContent =
    `${model} · ${r.inputTokens + r.outputTokens} tokens · US$ ${r.estimatedCostUsd.toFixed(4)} · ${(r.latencyMs / 1000).toFixed(1)} s`;
}
