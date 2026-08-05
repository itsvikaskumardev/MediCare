import React, { useState } from "react";
import { useNavigate } from "react-router-dom";
import toast, { Toaster } from "react-hot-toast";
import logo from "../../assets/logo.png";
import { ArrowLeft } from "lucide-react";
import { useAuth } from "../../context/AuthContext";

export default function Login() {
  const { login } = useAuth();
  const API_BASE = import.meta.env.VITE_BACKEND_URL || "http://localhost:5205";
  const [formData, setFormData] = useState({ email: "", password: "" });
  const [busy, setBusy] = useState(false);
  const navigate = useNavigate();

  const handleChange = (e) => {
    setFormData((s) => ({ ...s, [e.target.name]: e.target.value }));
  };

  const handleLogin = async (e) => {
    e.preventDefault();

    if (!formData.email || !formData.password) {
      toast.error("All fields are required!");
      return;
    }

    setBusy(true);
    try {
      const res = await fetch(`${API_BASE}/api/auth/login`, {
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

      // decode token to verify role
      const base64Url = token.split(".")[1];
      const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
      const jsonPayload = decodeURIComponent(atob(base64).split("").map(function (c) { return "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2); }).join(""));
      const decoded = JSON.parse(jsonPayload);
      
      const role = decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] || decoded.role;

      if (role !== "ADMIN") {
        toast.error("Access denied: You must be an Administrator to log in here.");
        setBusy(false);
        return;
      }

      login(token);

      toast.success("Login successful — redirecting...");

      setTimeout(() => {
        navigate("/h");
      }, 700);
    } catch (err) {
      console.error("login error", err);
      toast.error("Network error during login");
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <Toaster position="top-right" reverseOrder={false} />

      <button
        onClick={() => navigate("/")}
        className="absolute top-6 left-6 flex items-center gap-2 text-sm text-gray-500 hover:text-emerald-600 transition-colors"
      >
        <ArrowLeft size={16} />
        Back to Home
      </button>

      <div className="bg-white p-8 rounded-2xl shadow-xl w-full max-w-md border border-gray-100">
        <div className="flex justify-center mb-6">
          <img src={logo} alt="Logo" className="w-16 h-16 object-contain" />
        </div>

        <h2 className="text-2xl font-bold text-center text-gray-800 mb-2">Admin Portal</h2>
        <p className="text-center text-gray-500 text-sm mb-8">
          Sign in to manage the system
        </p>

        <form onSubmit={handleLogin} className="space-y-4">
          <input
            type="email"
            name="email"
            placeholder="Email Address"
            value={formData.email}
            onChange={handleChange}
            className="w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-emerald-500 bg-gray-50"
            required
          />

          <input
            type="password"
            name="password"
            placeholder="Password"
            value={formData.password}
            onChange={handleChange}
            className="w-full px-4 py-3 rounded-lg border border-gray-200 focus:outline-none focus:ring-2 focus:ring-emerald-500 bg-gray-50"
            required
          />

          <button
            type="submit"
            disabled={busy}
            className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-semibold rounded-lg shadow-md transition-colors disabled:opacity-50"
          >
            {busy ? "Signing in…" : "Login"}
          </button>
        </form>
      </div>
    </div>
  );
}
