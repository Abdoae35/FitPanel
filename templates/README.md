# Premium Fitness Coach Templates — HTML/CSS Files

Complete standalone HTML templates for your fitness coach platform PDF generation.

## 📁 Files

1. **`workout-plan-template.html`** — Workout plan with cover page + training days
2. **`diet-plan-template.html`** — Diet plan with cover page + meal plans

## ✨ Features

- ✅ **A4 Print-Ready** — Exact dimensions (210mm × 297mm / 794px × 1123px)
- ✅ **Self-Contained** — All CSS embedded, no external dependencies except Google Fonts
- ✅ **Placeholder System** — `{{VARIABLE}}` syntax for easy backend integration
- ✅ **Themeable** — CSS variables in `:root` for instant brand customization
- ✅ **Premium Design** — Dark athletic aesthetic with electric accents

---

## 🎨 Customizing Brand Colors

Open either HTML file and edit the `:root` CSS variables (around line 30):

```css
:root {
  /* Change these to match your coach's brand */
  --coach-primary: #00FF88;        /* Main accent color */
  --coach-secondary: #FF0066;      /* Secondary accent */
  --coach-dark: #0A0A0F;           /* Background color */
  --coach-dark-elevated: #1A1A24;  /* Card/table backgrounds */
  --coach-gray-light: #8A8A9E;     /* Muted text */
  
  /* Typography */
  --coach-font-display: 'Barlow Condensed', sans-serif;
  --coach-font-body: 'Inter', sans-serif;
  
  /* Spacing */
  --page-padding: 24px;
}
```

**Quick Color Presets:**
- **Cyber Blue**: Primary `#00D9FF`, Secondary `#9D00FF`
- **Fire Orange**: Primary `#FF6B00`, Secondary `#FF0040`
- **Gold Luxury**: Primary `#FFD700`, Secondary `#FF8C00`
- **Purple Power**: Primary `#A855F7`, Secondary `#EC4899`

---

## 🔧 Backend Integration

### Placeholder Replacement System

Both templates use `{{PLACEHOLDER}}` syntax. Replace these with your backend data:

#### **Workout Plan Placeholders**

**Cover Page:**
- `{{CLIENT_NAME}}` — Client's full name
- `{{START_DATE}}` — Plan start date
- `{{INSTAGRAM}}` — Coach Instagram handle
- `{{EMAIL}}` — Coach email
- `{{PHONE}}` — Coach phone number

**Workout Day Page (repeat for each day):**
- `{{DAY_NUMBER}}` — 1, 2, 3, etc.
- `{{DAY_TITLE}}` — "UPPER BODY POWER", "LEG DAY", etc.
- `{{SYSTEM_SUBTITLE}}` — Training protocol description
- `{{PHASE_LABEL}}` — "STRENGTH PHASE", "HYPERTROPHY", etc.

**Exercise Row (repeat for each exercise):**
```html
<tr>
  <td class="exercise-name">{{EXERCISE_NAME}}</td>
  <td style="text-align: center; font-weight: 700;">{{SETS}}</td>
  <td style="text-align: center;">{{REPS}}</td>
  <td style="text-align: center; color: var(--coach-primary);">{{REST}}</td>
</tr>
```

**Cardio Section (optional — remove entire `<div class="cardio-section">` if not needed):**
- `{{CARDIO_INTENSITY}}` — "LOW", "MODERATE", "HIGH"
- `{{CARDIO_TYPE}}` — "Treadmill", "Rowing", etc.
- `{{CARDIO_DURATION}}` — "20 minutes"
- `{{CARDIO_CONSTRAINTS}}` — "HR 120-140 BPM"

#### **Diet Plan Placeholders**

**Cover Page:**
- Same as workout plan + `{{COACH_NAME}}`, `{{COACH_SPECIALTY}}`, `{{COACH_BIO}}`
- `{{CERTIFICATION}}` — Repeat for each certification

**Diet Day Page:**
- `{{DAY_NUMBER}}` — Day number
- `{{DAY_TITLE}}` — "TRAINING DAY", "REST DAY", etc.
- `{{DIET_SUBTITLE}}` — Protocol description
- `{{PHASE_LABEL}}` — "MUSCLE GAIN PHASE", etc.

**Macros Summary:**
- `{{TOTAL_CALORIES}}`, `{{TOTAL_PROTEIN}}`, `{{TOTAL_CARBS}}`, `{{TOTAL_FATS}}`
- `{{PROTEIN_PERCENT}}`, `{{CARBS_PERCENT}}`, `{{FATS_PERCENT}}`

**Meal Card (repeat for each meal):**
- `{{MEAL_ICON}}` — 🍳, 🥤, 🍗, ⚡, 💪, 🍽️, 🌙
- `{{MEAL_NAME}}` — "MEAL 1 — BREAKFAST"
- `{{MEAL_TIME}}` — "7:00 AM"

**Food Item (repeat for each food):**
```html
<div class="food-item">
  <div class="food-name">{{FOOD_NAME}}</div>
  <div class="food-portion">{{PORTION}}</div>
  <div class="food-macros">
    <span class="macro-chip protein">P: {{PROTEIN}}g</span>
    <span class="macro-chip carbs">C: {{CARBS}}g</span>
    <span class="macro-chip fats">F: {{FATS}}g</span>
  </div>
</div>
```

**Notes:**
- `{{NOTES_TITLE}}` — Section title
- `{{NOTES_TEXT}}` — Notes content

---

## 📄 PDF Conversion

### Recommended Settings for Your HTML-to-PDF Engine

```javascript
// Example configuration (adjust for your backend)
{
  format: 'A4',
  printBackground: true,
  margin: {
    top: 0,
    right: 0,
    bottom: 0,
    left: 0
  },
  preferCSSPageSize: true
}
```

### Page Structure

Each `<div class="a4-page">` represents one A4 page. Your backend should:

1. **Cover Page**: Use once at the beginning
2. **Content Pages**: Duplicate the workout/diet day template for each day
3. **Concatenate**: Append all pages in sequence before PDF conversion

---

## 🧪 Testing

Open the HTML files directly in a browser to preview:

```bash
# From the templates directory
open workout-plan-template.html
open diet-plan-template.html
```

Or use Python's HTTP server:

```bash
cd templates
python3 -m http.server 8000
# Visit http://localhost:8000
```

---

## 📐 Design Specifications

- **Page Size**: A4 (210mm × 297mm / 794px × 1123px at 96dpi)
- **Safe Area**: 24px padding on all sides
- **Footer Height**: 60px fixed at bottom
- **Fonts**: Barlow Condensed (display), Inter (body)
- **Grid System**: CSS Grid for layouts
- **Color Mode**: Dark theme optimized

---

## 💡 Tips

### Handling Long Content

If a workout day has too many exercises to fit on one page, split into two pages:

```html
<!-- Page 1 -->
<div class="a4-page">
  <!-- Header + First 8 exercises -->
</div>

<!-- Page 2 -->
<div class="a4-page">
  <!-- Header (same) + Remaining exercises + Cardio -->
</div>
```

### Dynamic Coach Photo

Replace the Unsplash URL in the cover page:

```html
<img src="{{COACH_PHOTO_URL}}" alt="Coach Photo" class="coach-photo">
```

### Removing Optional Sections

**No Cardio?** Delete the entire `<div class="cardio-section">...</div>`

**No Notes?** Delete the entire `<div class="notes-section">...</div>`

---

## 🚀 Example Backend Workflow (Pseudocode)

```python
# 1. Read template
template = read_file('workout-plan-template.html')

# 2. Replace global placeholders
template = template.replace('{{CLIENT_NAME}}', client.name)
template = template.replace('{{START_DATE}}', plan.start_date)
# ... etc

# 3. Generate workout day pages
workout_pages = ''
for day in plan.workout_days:
    day_html = generate_workout_day_html(day)
    workout_pages += day_html

# 4. Inject pages into template
final_html = template.replace('<!-- INJECT PAGES HERE -->', workout_pages)

# 5. Convert to PDF
pdf = html_to_pdf(final_html, {
    'format': 'A4',
    'printBackground': True
})

# 6. Save/serve PDF
save_pdf(pdf, 'client-workout-plan.pdf')
```

---

## 📞 Support

For design questions or customization help, refer to the inline HTML comments or the CSS documentation at the top of each file.

**File Locations:**
- Workout Template: `templates/workout-plan-template.html`
- Diet Template: `templates/diet-plan-template.html`

**Last Updated:** May 21, 2026
