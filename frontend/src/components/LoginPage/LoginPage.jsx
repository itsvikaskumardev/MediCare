import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import toast, { Toaster } from "react-hot-toast";
import logo from "../../assets/logo.png";
import { ArrowLeft } from "lucide-react";
import { loginPageStyles, toastStyles } from "../../assets/dummyStyles";
import { useAuth } from "../../context/AuthContext";

export default function LoginPage({ apiBase }) {
  const { login } = useAuth();
  const API_BASE = apiBase || import.meta.env.BACKEND_URL || import.meta.env.VITE_BACKEND_URL || "http://localhost:5205";
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData((s) => ({ ...s, [e.target.name]: e.target.value }));
  };

  const handleLogin = async (e) => {
    e.preventDefault();

    if (!formData.email || !formData.password) {
      toast.error("All fields are required!", {
        style: toastStyles.errorToast,
      });
      return;
    }

    setBusy(true);
    try {
      const res = await fetch(`${API_BASE}/api/doctors/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(formData),
      });

      const json = await res.json().catch(() => null);

      if (!res.ok || json?.isSuccess === false) {
        toast.error(
          json?.errorMessages?.[0] || json?.message || "Login failed",
          { duration: 4000 }
        );
        setBusy(false);
        return;
      }

      const token = json?.result?.token || json?.token;

      if (!token) {
        toast.error("Authentication token missing");
        setBusy(false);
        return;
      }

      // decode token to find ID for routing
      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(atob(base64).split("").map(function (c) { return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2); }).join(""));
      const decoded = JSON.parse(jsonPayload);
      
      const doctorId = decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || decoded.nameid || decoded.id || decoded.sub;

      if (!doctorId) {
        toast.error("Doctor ID missing from token");
        setBusy(false);
        return;
      }

      login(token);

      toast.success("Login successful — redirecting...", {
        style: toastStyles.successToast,
      });

      // ✅ Navigate to dynamic route
      setTimeout(() => {
        navigate(`/doctor-admin/${doctorId}`);
      }, 700);
    } catch (err) {
      console.error("login error", err);
      toast.error("Network error during login");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className={loginPageStyles.mainContainer}>
      <Toaster position="top-right" reverseOrder={false} />

      <button
        onClick={() => navigate("/")}
        className={loginPageStyles.backButton}
      >
        <ArrowLeft className={loginPageStyles.backButtonIcon} />
        Back to Home
      </button>

      <div className={loginPageStyles.loginCard}>
        <div className={loginPageStyles.logoContainer}>
          <img src={logo} alt="Doctor Logo" className={loginPageStyles.logo} />
        </div>

        <h2 className={loginPageStyles.title}>Doctor Admin</h2>
        <p className={loginPageStyles.subtitle}>
          Sign in to manage your profile & schedule
        </p>

        <form onSubmit={handleLogin} className={loginPageStyles.form}>
          <input
            type="email"
            name="email"
            placeholder="Email Address"
            value={formData.email}
            onChange={handleChange}
            className={loginPageStyles.input}
            required
          />

          <input
            type="password"
            name="password"
            placeholder="Password"
            value={formData.password}
            onChange={handleChange}
            className={loginPageStyles.input}
            required
          />

          <button
            type="submit"
            disabled={busy}
            className={loginPageStyles.submitButton}
          >
            {busy ? "Signing in…" : "Login"}
          </button>
        </form>
      </div>
    </div>
  );
}
