export default function App() {
  return (
    <div className="min-h-screen bg-zinc-900 p-8">
      <div className="max-w-4xl mx-auto">
        <h1 className="text-4xl font-bold text-white mb-4">
          Premium Fitness Coach PDF Templates
        </h1>
        <p className="text-zinc-300 mb-8">
          View the print-ready templates below. Use your browser's print function or PDF export to generate the final documents.
        </p>
        
        <div className="space-y-4">
          <a 
            href="/workout-plan.html" 
            target="_blank"
            className="block bg-gradient-to-r from-orange-500 to-orange-600 hover:from-orange-600 hover:to-orange-700 text-white font-bold py-4 px-6 rounded-lg transition-all"
          >
            → Open Workout Plan Template
          </a>
          <a 
            href="/diet-plan.html" 
            target="_blank"
            className="block bg-gradient-to-r from-blue-500 to-blue-600 hover:from-blue-600 hover:to-blue-700 text-white font-bold py-4 px-6 rounded-lg transition-all"
          >
            → Open Diet Plan Template
          </a>
          <a 
            href="/templates.html" 
            target="_blank"
            className="block bg-gradient-to-r from-amber-500 to-amber-600 hover:from-amber-600 hover:to-amber-700 text-white font-bold py-4 px-6 rounded-lg transition-all"
          >
            → Open All Templates (Complete System)
          </a>
        </div>
        
        <div className="mt-12 bg-zinc-800 rounded-lg p-6">
          <h2 className="text-2xl font-bold text-white mb-4">Template Features</h2>
          <ul className="text-zinc-300 space-y-2">
            <li>✓ A4-sized pages (210mm × 297mm) ready for PDF export</li>
            <li>✓ Premium athletic design with dark backgrounds and neon accents</li>
            <li>✓ Customizable CSS variables for easy brand adaptation</li>
            <li>✓ Dynamic placeholders for coach and client data</li>
            <li>✓ Professional footer with contact information on every page</li>
            <li>✓ Print-optimized CSS with proper page breaks</li>
          </ul>
        </div>
      </div>
    </div>
  );
}
