function validatePassword(password) {
    const minLength = 12;
    const hasLower = /[a-z]/.test(password);
    const hasUpper = /[A-Z]/.test(password);
    const hasNumber = /\d/.test(password);
    const hasSpecial = /[!@#$%^&*(),.?":{}|<>]/.test(password);
    
    let strength = 0;
    let feedback = [];
    
    if (password.length >= minLength) {
        strength++;
    } else {
        feedback.push("at least 12 characters");
    }
    
    if (hasLower) {
        strength++;
    } else {
        feedback.push("lowercase letter");
    }
    
    if (hasUpper) {
        strength++;
    } else {
        feedback.push("uppercase letter");
    }
    
    if (hasNumber) {
        strength++;
    } else {
        feedback.push("number");
    }
    
    if (hasSpecial) {
        strength++;
    } else {
        feedback.push("special character");
    }
    
    const isValid = strength === 5;
    
    return {
        isValid: isValid,
        strength: strength,
        feedback: feedback,
        message: isValid ? "Strong password!" : "Missing: " + feedback.join(", ")
    };
}

// Input sanitization
function sanitizeInput(input) {
    const div = document.createElement('div');
    div.textContent = input;
    return div.innerHTML;
}

// Email validation
function validateEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

// NRIC validation
function validateNRIC(nric) {
    const regex = /^[STFG]\d{7}[A-Z]$/;
    return regex.test(nric);
}