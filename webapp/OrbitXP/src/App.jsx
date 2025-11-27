import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import Header from './components/Header';
import TermsOfService from './pages/TermsOfService';
import PrivacyPolicy from './pages/PrivacyPolicy';

function App() {
  return (
    <Router>
      <div className="flex flex-col min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-800 dark:text-gray-200">
        <Header />
        <main className="flex-grow container mx-auto px-6 py-8">
          <Routes>
            <Route path="/terms-of-service" element={<TermsOfService />} />
            <Route path="/privacy-policy" element={<PrivacyPolicy />} />
            <Route
              path="/"
              element={
                <div className="text-center">
                  <h1 className="text-4xl font-bold mb-4">Welcome to OrbitXP</h1>
                  <p className="text-lg">Your hub for global activity stats.</p>
                </div>
              }
            />
          </Routes>
        </main>
        <footer className="bg-gray-900 text-white py-4">
          <div className="container mx-auto px-6 text-center">
            <p>&copy; {new Date().getFullYear()} OrbitXP. All rights reserved.</p>
          </div>
        </footer>
      </div>
    </Router>
  );
}

export default App;
