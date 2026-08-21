"use client";

import React, { useEffect, useRef, useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import logo from "../../assets/logo.png";
import { Menu, X, User as UserIcon, Key, LogOut } from "lucide-react";
import PatientLoginModal from "../PatientLoginModal/PatientLoginModal";
import { useAuth } from "../../context/AuthContext";
import { navbarStyles } from "../../assets/dummyStyles";

export default function Navbar() {
  const [isOpen, setIsOpen] = useState(false);
  const [showNavbar, setShowNavbar] = useState(true);
  const [lastScrollY, setLastScrollY] = useState(0);
  const [isPatientModalOpen, setIsPatientModalOpen] = useState(false);

  const { user, logout } = useAuth();

  const location = useLocation();
  const navRef = useRef(null);
  const navigate = useNavigate();

  /* Hide / show navbar on scroll */
  useEffect(() => {
    const handleScroll = () => {
      const currentScrollY = window.scrollY;
      if (currentScrollY > lastScrollY && currentScrollY > 80) {
        setShowNavbar(false);
      } else {
        setShowNavbar(true);
      }
      setLastScrollY(currentScrollY);
    };
    window.addEventListener("scroll", handleScroll, { passive: true });
    return () => window.removeEventListener("scroll", handleScroll);
  }, [lastScrollY]);

  /* Close mobile menu on outside click */
  useEffect(() => {
    const handleClickOutside = (event) => {
      if (isOpen && navRef.current && !navRef.current.contains(event.target)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  const navItems = [
    { label: "Home", href: "/" },
    { label: "Doctors", href: "/doctors" },
    { label: "Services", href: "/services" },
    { label: "Appointments", href: "/appointments" },
    { label: "Contact", href: "/contact" },
  ];

  return (
    <>
      <div className={navbarStyles.navbarBorder} />

      <nav
        ref={navRef}
        className={`${navbarStyles.navbarContainer} ${
          showNavbar ? navbarStyles.navbarVisible : navbarStyles.navbarHidden
        }`}
      >
        <div className={navbarStyles.contentWrapper}>
          <div className={navbarStyles.flexContainer}>
            {/* Logo */}
            <Link to="/" className={navbarStyles.logoLink}>
              <div className={navbarStyles.logoContainer}>
                <div className={navbarStyles.logoImageWrapper}>
                  <img
                    src={logo}
                    alt="MedBook logo"
                    className={navbarStyles.logoImage}
                  />
                </div>
              </div>
              <div className={navbarStyles.logoTextContainer}>
                <h1 className={navbarStyles.logoTitle}>
                  MediCare
                </h1>
                <p className={navbarStyles.logoSubtitle}>
                  Healthcare Solutions
                </p>
              </div>
            </Link>

            {/* Desktop navigation */}
            <div className={navbarStyles.desktopNav}>
              <div className={navbarStyles.navItemsContainer}>
                {navItems.map((item) => {
                  const isActive = location.pathname === item.href;
                  return (
                    <Link
                      key={item.href}
                      to={item.href}
                      className={`${navbarStyles.navItem} ${
                        isActive
                          ? navbarStyles.navItemActive
                          : navbarStyles.navItemInactive
                      }`}
                    >
                      {item.label}
                    </Link>
                  );
                })}
              </div>
            </div>

            {/* Right side */}
            <div className={navbarStyles.rightContainer}>
              {/* ================= PATIENT LOGGED OUT ================= */}
              {/* Doctor Admin */}
              {(!user || user.role !== "PATIENT") && (
                <Link
                  to={user?.role === "DOCTOR" || user?.role === "ADMIN" ? `/doctor-admin/${user.id}` : "/doctor-admin/login"}
                  className={navbarStyles.doctorAdminButton}
                >
                  <UserIcon className={navbarStyles.doctorAdminIcon} />
                  <span className={navbarStyles.doctorAdminText}>
                    {user?.role === "DOCTOR" || user?.role === "ADMIN" ? "Dashboard" : "Doctor Admin"}
                  </span>
                </Link>
              )}

              {/* Patient Login or Profile */}
              {!user ? (
                <button
                  type="button"
                  onClick={() => setIsPatientModalOpen(true)}
                  className={navbarStyles.loginButton}
                >
                  <Key className={navbarStyles.loginIcon} />
                  Login
                </button>
              ) : (
                <div className="hidden lg:flex items-center gap-2">
                  <Link to="/my-profile" className="flex items-center gap-2 px-3 py-1.5 bg-emerald-50 hover:bg-emerald-100 text-emerald-800 rounded-full text-xs font-medium border border-emerald-200 transition-colors cursor-pointer" title="My Profile">
                    <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                    <span className="max-w-[120px] truncate">{user.fullName || user.email}</span>
                  </Link>
                  <button
                    type="button"
                    onClick={logout}
                    className={navbarStyles.signOutButton}
                  >
                    Sign Out
                  </button>
                </div>
              )}

              {/* Mobile/Tablet toggle */}
              <button
                onClick={() => setIsOpen(!isOpen)}
                className={navbarStyles.mobileToggle}
                aria-expanded={isOpen}
                aria-label="Open menu"
              >
                {isOpen ? (
                  <X className={navbarStyles.toggleIcon} />
                ) : (
                  <Menu className={navbarStyles.toggleIcon} />
                )}
              </button>
            </div>
          </div>

          {/* Mobile/Tablet menu */}
          {isOpen && (
            <div className={navbarStyles.mobileMenu}>
              {navItems.map((item, idx) => {
                const isActive = location.pathname === item.href;
                return (
                  <Link
                    key={idx}
                    to={item.href}
                    onClick={() => setIsOpen(false)}
                    className={`${navbarStyles.mobileMenuItem} ${
                      isActive
                        ? navbarStyles.mobileMenuItemActive
                        : navbarStyles.mobileMenuItemInactive
                    }`}
                  >
                    {item.label}
                  </Link>
                );
              })}
              {/* Patient logged out */}
              {(!user || user.role !== "PATIENT") && (
                <Link
                  to={user?.role === "DOCTOR" || user?.role === "ADMIN" ? `/doctor-admin/${user.id}` : "/doctor-admin/login"}
                  onClick={() => setIsOpen(false)}
                  className={navbarStyles.mobileDoctorAdminButton}
                >
                  {user?.role === "DOCTOR" || user?.role === "ADMIN" ? "Dashboard" : "Doctor Admin"}
                </Link>
              )}
              <div className={navbarStyles.mobileLoginContainer}>
                {!user ? (
                  <button
                    type="button"
                    onClick={() => {
                      setIsOpen(false);
                      setIsPatientModalOpen(true);
                    }}
                    className={navbarStyles.mobileLoginButton}
                  >
                    Login
                  </button>
                ) : (
                  <div className="flex flex-col gap-2 w-full">
                    <div className="px-3 py-2 bg-emerald-50 text-emerald-800 rounded-lg text-xs font-medium text-center">
                      Logged in as {user.fullName || user.email}
                    </div>
                    <button
                      type="button"
                      onClick={() => {
                        logout();
                        setIsOpen(false);
                      }}
                      className={navbarStyles.mobileSignOutButton}
                    >
                      Sign Out
                    </button>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
        {/* Animations */}
        <style>{navbarStyles.animationStyles}</style>
      </nav>

      <PatientLoginModal
        isOpen={isPatientModalOpen}
        onClose={() => setIsPatientModalOpen(false)}
      />
    </>
  );
}