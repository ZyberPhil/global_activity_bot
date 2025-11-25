import { BrowserRouter as Router, Routes, Route, Link } from 'react-router-dom';
import './App.css';
import TermsOfService from './pages/TermsOfService';
import PrivacyPolicy from './pages/PrivacyPolicy';

function App() {
  return (
    <Router>
      <div className="App">
        <nav>
          <ul className="nav-list">
            <li>
              <Link to="/terms-of-service">Terms of Service</Link>
            </li>
            <li>
              <Link to="/privacy-policy">Privacy Policy</Link>
            </li>
          </ul>
        </nav>
        <main>
          <Routes>
            <Route path="/terms-of-service" element={<TermsOfService />} />
            <Route path="/privacy-policy" element={<PrivacyPolicy />} />
            <Route
              path="/"
              element={
                <div className="home">
                  <h1>Welcome to OrbitXP Legal</h1>
                  <p>Select a page from the navigation.</p>
                </div>
              }
            />
          </Routes>
        </main>
      </div>
    </Router>
  );
}

export default App;
