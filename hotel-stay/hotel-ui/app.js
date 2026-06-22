const API = 'http://localhost:5000';

// City classification for client-side doc validation
const INTERNATIONAL = ['Paris', 'London', 'New York', 'Dubai', 'Tokyo', 'Sydney'];
const DOMESTIC      = ['Mumbai', 'Delhi', 'Bangalore', 'Chennai', 'Hyderabad'];

let currentResults   = [];
let selectedRoom     = null;
let currentSearch    = {};

// ── SEARCH ────────────────────────────────────────────────────────────────────
async function handleSearch(e) {
  e.preventDefault();
  const btn  = document.getElementById('searchBtn');
  const errEl = document.getElementById('searchError');
  errEl.style.display = 'none';

  const destination = document.getElementById('destination').value;
  const checkIn     = document.getElementById('checkIn').value;
  const checkOut    = document.getElementById('checkOut').value;
  const roomType    = document.getElementById('roomType').value;

  // Client-side date validation
  if (new Date(checkOut) <= new Date(checkIn)) {
    showError(errEl, 'Check-out must be after check-in.');
    return;
  }

  currentSearch = { destination, checkIn, checkOut, roomType };

  btn.disabled = true;
  btn.textContent = 'Searching…';

  try {
    let url = `${API}/hotels/search?destination=${encodeURIComponent(destination)}&checkIn=${checkIn}&checkOut=${checkOut}`;
    if (roomType) url += `&roomType=${roomType}`;

    const res  = await fetch(url);
    const data = await res.json();

    if (!res.ok) { showError(errEl, data.error || 'Search failed.'); return; }

    currentResults = data;
    renderResults();
  } catch {
    showError(errEl, 'Could not connect to the API. Make sure the backend is running on port 5000.');
  } finally {
    btn.disabled = false;
    btn.textContent = 'Search Hotels';
  }
}

// ── RENDER RESULTS ────────────────────────────────────────────────────────────
function renderResults() {
  const section  = document.getElementById('resultsSection');
  const grid     = document.getElementById('resultsGrid');
  const title    = document.getElementById('resultsTitle');
  const empty    = document.getElementById('emptyState');

  if (!currentResults.length) {
    section.style.display = 'none';
    empty.style.display   = 'block';
    return;
  }

  empty.style.display   = 'none';
  section.style.display = 'block';

  const nights = nightsBetween(currentSearch.checkIn, currentSearch.checkOut);
  title.textContent = `${currentResults.length} room${currentResults.length !== 1 ? 's' : ''} found in ${currentSearch.destination} · ${nights} night${nights !== 1 ? 's' : ''}`;

  sortResults();
}

function sortResults() {
  const sort = document.getElementById('sortSelect').value;
  const sorted = [...currentResults].sort((a, b) => {
    if (sort === 'price-asc')  return a.totalPrice - b.totalPrice;
    if (sort === 'price-desc') return b.totalPrice - a.totalPrice;
    if (sort === 'provider')   return a.provider.localeCompare(b.provider);
    return 0;
  });

  const grid = document.getElementById('resultsGrid');
  grid.innerHTML = sorted.map(roomCard).join('');
}

function roomCard(r) {
  const stars      = r.starRating ? '⭐'.repeat(r.starRating) : '';
  const badgeCls   = r.provider === 'PremierStays' ? 'badge-premier' : 'badge-budget';
  const policyCls  = r.cancellationPolicy === 'FreeCancellation' ? 'policy-free'
                   : r.cancellationPolicy === 'Flexible'         ? 'policy-flexible'
                   : 'policy-nonref';
  const policyIcon = r.cancellationPolicy === 'FreeCancellation' ? '✅'
                   : r.cancellationPolicy === 'Flexible'         ? '🔄'
                   : '❌';
  const amenities  = r.amenities?.length
    ? r.amenities.map(a => `<span class="amenity-tag">${a}</span>`).join('')
    : '';

  return `
  <div class="room-card">
    <div class="card-header">
      <div>
        <div class="room-type">${r.roomType}</div>
        ${stars ? `<div class="stars">${stars}</div>` : ''}
      </div>
      <span class="provider-badge ${badgeCls}">${r.provider}</span>
    </div>
    <div class="price-block">
      <div class="price-total">₹${fmt(r.totalPrice)}</div>
      <div class="price-night">₹${fmt(r.ratePerNight)} / night · ${r.nights} night${r.nights !== 1 ? 's' : ''}</div>
    </div>
    <span class="policy-badge ${policyCls}">${policyIcon} ${r.cancellationPolicy}</span>
    ${amenities ? `<div class="amenities">${amenities}</div>` : ''}
    <button class="btn-reserve" onclick='openReserveModal(${JSON.stringify(r)})'>Reserve Now</button>
  </div>`;
}

// ── RESERVE MODAL ─────────────────────────────────────────────────────────────
function openReserveModal(room) {
  selectedRoom = room;
  const destination = currentSearch.destination;
  const isIntl = INTERNATIONAL.includes(destination);

  // Pre-select recommended doc type
  const docTypeEl = document.getElementById('docType');
  docTypeEl.value = isIntl ? 'Passport' : 'NationalId';

  document.getElementById('reserveSummary').innerHTML = `
    <strong>${room.provider}</strong> · ${room.roomType}<br/>
    📍 ${destination} &nbsp;|&nbsp; 📅 ${currentSearch.checkIn} → ${currentSearch.checkOut}<br/>
    💰 <strong>₹${fmt(room.totalPrice)}</strong> total (₹${fmt(room.ratePerNight)}/night)<br/>
    ${room.cancellationPolicy === 'FreeCancellation' ? '✅ Free Cancellation'
      : room.cancellationPolicy === 'Flexible' ? '🔄 Flexible'
      : '❌ Non-Refundable'}`;

  document.getElementById('reserveError').style.display = 'none';
  document.getElementById('docWarning').style.display   = 'none';
  document.getElementById('guestName').value   = '';
  document.getElementById('docNumber').value   = '';

  validateDocClient();
  document.getElementById('reserveModal').classList.add('show');
}

function closeReserveModal() {
  document.getElementById('reserveModal').classList.remove('show');
}

// Client-side document validation warning
function validateDocClient() {
  const destination = currentSearch.destination;
  const docType     = document.getElementById('docType').value;
  const warn        = document.getElementById('docWarning');

  if (INTERNATIONAL.includes(destination) && docType === 'NationalId') {
    warn.textContent = '⚠️ International destinations require a Passport.';
    warn.style.display = 'block';
  } else {
    warn.style.display = 'none';
  }
}

// ── HANDLE RESERVE ────────────────────────────────────────────────────────────
async function handleReserve(e) {
  e.preventDefault();
  const errEl = document.getElementById('reserveError');
  errEl.style.display = 'none';

  const payload = {
    destination:    currentSearch.destination,
    checkIn:        currentSearch.checkIn,
    checkOut:       currentSearch.checkOut,
    roomType:       selectedRoom.roomType,
    providerId:     selectedRoom.provider,
    guestName:      document.getElementById('guestName').value.trim(),
    documentType:   document.getElementById('docType').value,
    documentNumber: document.getElementById('docNumber').value.trim(),
  };

  try {
    const res  = await fetch(`${API}/hotels/reserve`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body:    JSON.stringify(payload),
    });
    const data = await res.json();

    if (!res.ok) {
      showError(errEl, data.error || `Error ${res.status}`);
      return;
    }

    closeReserveModal();
    showConfirmation(data);
  } catch {
    showError(errEl, 'Could not connect to the API.');
  }
}

// ── CONFIRMATION ──────────────────────────────────────────────────────────────
function showConfirmation(r) {
  document.getElementById('confirmDetails').innerHTML = `
    <span class="ref-number">${r.referenceNumber}</span>
    <strong>Guest:</strong> ${r.guestName}<br/>
    <strong>Provider:</strong> ${r.provider}<br/>
    <strong>Destination:</strong> ${r.destination}<br/>
    <strong>Room:</strong> ${r.roomType}<br/>
    <strong>Dates:</strong> ${r.checkIn} → ${r.checkOut}<br/>
    <strong>Total:</strong> ₹${fmt(r.totalPrice)}<br/>
    <strong>Cancellation:</strong> ${r.cancellationPolicy}`;
  document.getElementById('confirmModal').classList.add('show');
}

function closeConfirmModal() {
  document.getElementById('confirmModal').classList.remove('show');
}

// ── HELPERS ───────────────────────────────────────────────────────────────────
function nightsBetween(ci, co) {
  return Math.round((new Date(co) - new Date(ci)) / 86400000);
}

function fmt(n) {
  return Number(n).toLocaleString('en-IN', { minimumFractionDigits: 0 });
}

function showError(el, msg) {
  el.textContent = msg;
  el.style.display = 'block';
}

// Set min date for check-in to today
window.addEventListener('DOMContentLoaded', () => {
  const today = new Date().toISOString().split('T')[0];
  document.getElementById('checkIn').min  = today;
  document.getElementById('checkOut').min = today;
  document.getElementById('checkIn').addEventListener('change', e => {
    document.getElementById('checkOut').min = e.target.value;
  });
});
