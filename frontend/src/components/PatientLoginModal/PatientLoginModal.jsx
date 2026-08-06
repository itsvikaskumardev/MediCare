import React, { useState } from "react";
import toast, { Toaster } from "react-hot-toast";
import { X, Mail, Lock, User, ArrowRight, ShieldCheck, AlertCircle } from "lucide-react";
import { useAuth } from "../../context/AuthContext";

export default function PatientLoginModal({ isOpen, onClose, onLoginSuccess }) {
  const { login } = useAuth();
  const API_BASE =
    import.meta.env.BACKEND_URL ||
    import.meta.env.VITE_BACKEND_URL ||
    "http://localhost:5205";

  const [activeTab, setActiveTab] = useState("signin"); // "signin" | "signup"
  const [busy, setBusy] = useState(false);
  const [errorMsg, setErrorMsg] = useState("");

  // Form states
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    password: "",
    mobile: "",
    age: "",
    gender: "",
    bloodGroup: "",
    medicalHistory: "",
    allergies: "",
    emergencyContactName: "",
    emergencyContactNumber: ""
  });

  if (!isOpen) return null;

  const handleChange = (e) => {
    setErrorMsg("");
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSignIn = async (e) => {
    e.preventDefault();
    if (!formData.email || !formData.password) {
      toast.error("Please enter both email and password.");
      return;
    }

    setBusy(true);
    try {
      const res = await fetch(`${API_BASE}/api/auth/login`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          email: formData.email,
          password: formData.password,
        }),
      });

      const json = await res.json().catch(() => null);

      if (!res.ok || !json?.isSuccess) {
        const msg = json?.errorMessages?.[0] || json?.message || "Invalid email or password";
        setErrorMsg(msg);
        toast.error(msg);
        setBusy(false);
        return;
      }

      const { email, token, role } = json.result || {};
      if (!token) {
        toast.error("No token received from server.");
        setBusy(false);
        return;
      }

      // Use central auth context
      login(token);

      toast.success("Login successful!");
      setBusy(false);

      if (onLoginSuccess) onLoginSuccess({ email, role, token });
      onClose();
    } catch (err) {
      console.error("Login error:", err);
      toast.error("Could not connect to authentication server.");
      setBusy(false);
    }
  };

  const handleSignUp = async (e) => {
    e.preventDefault();
    if (!formData.name || !formData.email || !formData.password) {
      toast.error("Please fill in all fields.");
      return;
    }

    setBusy(true);
    try {
      const res = await fetch(`${API_BASE}/api/auth/register-patient`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: formData.name,
          email: formData.email,
          password: formData.password,
          mobile: formData.mobile || undefined,
          age: formData.age ? parseInt(formData.age, 10) : undefined,
          gender: formData.gender || undefined,
          bloodGroup: formData.bloodGroup || undefined,
          medicalHistory: formData.medicalHistory || undefined,
          allergies: formData.allergies || undefined,
          emergencyContactName: formData.emergencyContactName || undefined,
          emergencyContactNumber: formData.emergencyContactNumber || undefined
        }),
      });

      const json = await res.json().catch(() => null);

      if (!res.ok || !json?.isSuccess) {
        const msg = json?.errorMessages?.[0] || json?.message || "Registration failed";
        setErrorMsg(msg);
        toast.error(msg);
        setBusy(false);
        return;
      }

      toast.success("Account created successfully! Please sign in.");
      setActiveTab("signin");
      setBusy(false);
    } catch (err) {
      console.error("Registration error:", err);
      toast.error("Could not connect to authentication server.");
      setBusy(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-fadeIn">
      <div
        className={`relative w-full max-h-[95vh] overflow-y-auto bg-white rounded-2xl shadow-2xl border border-gray-100 transition-all duration-300 ${
          activeTab === "signup" ? "max-w-3xl" : "max-w-md"
        }`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header decoration */}
        <div className="bg-gradient-to-r from-emerald-600 to-teal-600 px-6 py-8 text-white text-center relative">
          <button
            onClick={onClose}
            className="absolute top-4 right-4 p-1.5 rounded-full bg-white/10 hover:bg-white/20 text-white transition-colors"
            title="Close"
          >
            <X size={18} />
          </button>
          <div className="w-12 h-12 bg-white/20 rounded-full flex items-center justify-center mx-auto mb-3 backdrop-blur-md">
            <ShieldCheck size={26} />
          </div>
          <h2 className="text-2xl font-bold">Patient Portal</h2>
          <p className="text-emerald-100 text-sm mt-1">
            {activeTab === "signin"
              ? "Sign in to manage your appointments"
              : "Create a patient account to get started"}
          </p>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-100">
          <button
            type="button"
            onClick={() => setActiveTab("signin")}
            className={`flex-1 py-3.5 text-sm font-semibold transition-colors relative ${
              activeTab === "signin"
                ? "text-emerald-600"
                : "text-gray-500 hover:text-gray-800"
            }`}
          >
            Sign In
            {activeTab === "signin" && (
              <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-emerald-600" />
            )}
          </button>
          <button
            type="button"
            onClick={() => setActiveTab("signup")}
            className={`flex-1 py-3.5 text-sm font-semibold transition-colors relative ${
              activeTab === "signup"
                ? "text-emerald-600"
                : "text-gray-500 hover:text-gray-800"
            }`}
          >
            Create Account
            {activeTab === "signup" && (
              <span className="absolute bottom-0 left-0 right-0 h-0.5 bg-emerald-600" />
            )}
          </button>
        </div>

        {/* Body Form */}
        <Toaster position="top-right" reverseOrder={false} />
        {errorMsg && (
          <div className="mx-6 mt-4 p-3 bg-red-50 border border-red-200 text-red-700 rounded-lg text-xs font-medium flex items-center gap-2 animate-fadeIn">
            <AlertCircle size={16} className="shrink-0 text-red-500" />
            <span>{errorMsg}</span>
          </div>
        )}
        <div className="p-6">
          {activeTab === "signin" ? (
            <form onSubmit={handleSignIn} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">
                  Email Address
                </label>
                <div className="relative">
                  <Mail
                    size={18}
                    className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400"
                  />
                  <input
                    type="email"
                    name="email"
                    required
                    placeholder="you@example.com"
                    value={formData.email}
                    onChange={handleChange}
                    className="w-full pl-10 pr-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">
                  Password
                </label>
                <div className="relative">
                  <Lock
                    size={18}
                    className="absolute left-3.5 top-1/2 -translate-y-1/2 text-gray-400"
                  />
                  <input
                    type="password"
                    name="password"
                    required
                    placeholder="••••••••"
                    value={formData.password}
                    onChange={handleChange}
                    className="w-full pl-10 pr-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={busy}
                className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-medium text-sm rounded-lg shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 transition-all disabled:opacity-50"
              >
                {busy ? "Signing In..." : "Sign In"}
                {!busy && <ArrowRight size={16} />}
              </button>
            </form>
          ) : (
            <form onSubmit={handleSignUp} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Name */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Full Name *</label>
                  <input type="text" name="name" required placeholder="John Doe" value={formData.name} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Email */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Email Address *</label>
                  <input type="email" name="email" required placeholder="you@example.com" value={formData.email} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Password */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Password *</label>
                  <input type="password" name="password" required placeholder="Create a password" value={formData.password} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Mobile */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Mobile</label>
                  <input type="text" name="mobile" placeholder="Mobile Number" value={formData.mobile} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Age */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Age</label>
                  <input type="number" name="age" min="0" placeholder="Age" value={formData.age} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Gender */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Gender</label>
                  <select name="gender" value={formData.gender} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all">
                    <option value="">Select Gender</option>
                    <option value="Male">Male</option>
                    <option value="Female">Female</option>
                    <option value="Other">Other</option>
                  </select>
                </div>
                {/* Blood Group */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Blood Group</label>
                  <select name="bloodGroup" value={formData.bloodGroup} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all">
                    <option value="">Select Blood Group</option>
                    <option value="A+">A+</option>
                    <option value="A-">A-</option>
                    <option value="B+">B+</option>
                    <option value="B-">B-</option>
                    <option value="O+">O+</option>
                    <option value="O-">O-</option>
                    <option value="AB+">AB+</option>
                    <option value="AB-">AB-</option>
                  </select>
                </div>
                {/* Emergency Contact Name */}
                <div>
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Emergency Contact Name</label>
                  <input type="text" name="emergencyContactName" placeholder="Name" value={formData.emergencyContactName} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Emergency Contact Number */}
                <div className="md:col-span-2">
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Emergency Contact Number</label>
                  <input type="text" name="emergencyContactNumber" placeholder="Number" value={formData.emergencyContactNumber} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all" />
                </div>
                {/* Medical History */}
                <div className="md:col-span-2">
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Medical History</label>
                  <textarea name="medicalHistory" rows="2" placeholder="Any past medical conditions..." value={formData.medicalHistory} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all"></textarea>
                </div>
                {/* Allergies */}
                <div className="md:col-span-2">
                  <label className="block text-xs font-semibold uppercase text-gray-500 mb-1">Allergies</label>
                  <textarea name="allergies" rows="2" placeholder="Any known allergies..." value={formData.allergies} onChange={handleChange} className="w-full px-4 py-2.5 bg-gray-50 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-emerald-500 focus:bg-white transition-all"></textarea>
                </div>
              </div>

              <div className="pt-2">
                <button
                  type="submit"
                  disabled={busy}
                  className="w-full py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-medium text-sm rounded-lg shadow-md shadow-emerald-600/20 flex items-center justify-center gap-2 transition-all disabled:opacity-50"
                >
                  {busy ? "Creating Account..." : "Create Account"}
                  {!busy && <ArrowRight size={16} />}
                </button>
              </div>
            </form>
          )}
        </div>

        {/* Footer note */}
        <div className="bg-gray-50 px-6 py-3 border-t border-gray-100 text-center text-xs text-gray-500">
          Secured with JWT authentication
        </div>
      </div>
    </div>
  );
}
