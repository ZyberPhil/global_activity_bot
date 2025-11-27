import { Link } from 'react-router-dom';

const Header = () => {
  return (
    <header className="bg-gray-900 text-white shadow-lg">
      <div className="container mx-auto px-6 py-4 flex justify-between items-center">
        <Link to="/" className="text-2xl font-bold">
          OrbitXP
        </Link>
        <nav>
          <ul className="flex space-x-4">
            <li>
              <Link to="/terms-of-service" className="hover:text-gray-300">Terms of Service</Link>
            </li>
            <li>
              <Link to="/privacy-policy" className="hover:text-gray-300">Privacy Policy</Link>
            </li>
          </ul>
        </nav>
      </div>
    </header>
  );
};

export default Header;
