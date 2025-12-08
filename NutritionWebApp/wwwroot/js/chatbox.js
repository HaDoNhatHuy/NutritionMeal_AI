// ====================================
// CHATBOX - FIXED SCROLLING ISSUES
// ====================================

const chatIcon = document.getElementById('chatIcon');
const chatPopup = document.getElementById('chatPopup');
const closeChatBtn = document.getElementById('closeChatBtn');
const chatBody = document.getElementById('chatBody');
const chatInput = document.getElementById('chatInput');
const sendBtn = document.getElementById('sendBtn');
const proactiveAdviceBtn = document.getElementById('proactiveAdviceBtn');
const chatModeContent = document.getElementById('chatModeContent');
const recipeModeContent = document.getElementById('recipeModeContent');
const showChatBtn = document.getElementById('showChatBtn');
const showRecipeBtn = document.getElementById('showRecipeBtn');

let isWaitingForAI = false;

// Format markdown content in AI messages
function formatMarkdown(text) {
    if (!text) return '';
    let formatted = text;

    // Bold: **text**
    formatted = formatted.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');

    // Lists: - item or * item
    formatted = formatted.replace(/^\s*[\*-]\s+(.*)$/gm, '<li>$1</li>');
    formatted = formatted.replace(/(<li>.*?<\/li>(\s*<br>\s*|)*)+/g, (match) => {
        let listContent = match.replace(/<br>/g, '');
        return `<ul>${listContent}</ul>`;
    });

    // Code: `code`
    formatted = formatted.replace(/`(.*?)`/g, '<code>$1</code>');

    // Line breaks
    formatted = formatted.replace(/\n\n/g, '</p><p>');
    formatted = formatted.replace(/\n/g, '<br>');

    // Wrap in paragraph
    if (!formatted.startsWith('<ul') && !formatted.startsWith('<p>')) {
        formatted = '<p>' + formatted + '</p>';
    }

    return formatted;
}

// CRITICAL FIX: Force immediate scroll to bottom
function scrollToBottom() {
    if (!chatBody) return;

    // Method 1: Direct scroll
    chatBody.scrollTop = chatBody.scrollHeight;

    // Method 2: Smooth scroll with requestAnimationFrame
    requestAnimationFrame(() => {
        chatBody.scrollTop = chatBody.scrollHeight;

        // Method 3: Double-check after a short delay
        setTimeout(() => {
            chatBody.scrollTop = chatBody.scrollHeight;
        }, 50);
    });
}

// Switch between chat and recipe modes
function switchChatMode(mode) {
    if (mode === 'chat') {
        chatModeContent.style.display = 'flex';
        recipeModeContent.style.display = 'none';
        showChatBtn.classList.add('btn-warning');
        showRecipeBtn.classList.remove('btn-warning');
    } else if (mode === 'recipe') {
        chatModeContent.style.display = 'none';
        recipeModeContent.style.display = 'flex';
        showRecipeBtn.classList.add('btn-warning');
        showChatBtn.classList.remove('btn-warning');
    }
}

// Open/Close chat
chatIcon.addEventListener('click', function () {
    chatPopup.classList.toggle('open');
    if (chatPopup.classList.contains('open')) {
        switchChatMode('chat');
        loadChatHistory();
        setTimeout(() => {
            chatInput.focus();
            scrollToBottom();
        }, 300);
    }
});

closeChatBtn.addEventListener('click', function () {
    chatPopup.classList.remove('open');
});

// Close when clicking outside
document.addEventListener('click', function (e) {
    if (!chatPopup.contains(e.target) && !chatIcon.contains(e.target) && chatPopup.classList.contains('open')) {
        chatPopup.classList.remove('open');
    }
});

// FIXED: Append message to chat with proper scrolling
function appendMessage(sender, message, isTyping = false) {
    const msgDiv = document.createElement('div');
    msgDiv.className = `chat-message ${sender === 'user' ? 'user-message' : 'ai-message'}`;

    if (isTyping) {
        msgDiv.id = 'typingIndicator';
        msgDiv.innerHTML = `
            <span class="typing-dot"></span>
            <span class="typing-dot"></span>
            <span class="typing-dot"></span>
        `;
    } else {
        let formattedMessage = formatMarkdown(message);
        msgDiv.innerHTML = formattedMessage;
    }

    chatBody.appendChild(msgDiv);

    // FIXED: Force scroll after append
    setTimeout(() => scrollToBottom(), 50);

    return msgDiv;
}

// FIXED: Typing animation with continuous scrolling
function typeResponse(element, text) {
    let index = 0;
    element.innerHTML = '';

    const interval = setInterval(() => {
        if (index < text.length) {
            const char = text.charAt(index);
            element.innerHTML += char === '\n' ? '<br>' : char;

            // FIXED: Scroll continuously during typing
            scrollToBottom();

            index++;
        } else {
            element.innerHTML = formatMarkdown(text);
            clearInterval(interval);
            isWaitingForAI = false;
            sendBtn.disabled = false;
            chatInput.disabled = false;

            // FIXED: Final scroll and focus input
            scrollToBottom();
            setTimeout(() => chatInput.focus(), 100);
        }
    }, 20);
}

// Load initial greeting
function loadChatHistory() {
    if (chatBody.children.length === 0) {
        appendMessage('ai', 'Chào bạn! 👋 Tôi là **NutritionAI**. Hãy cho tôi biết bạn muốn hỏi gì về chế độ ăn uống hoặc mục tiêu của mình nhé.', false);
    }
}

// FIXED: Send message function with proper scrolling
async function sendMessage() {
    const question = chatInput.value.trim();
    if (question === '' || isWaitingForAI) return;

    appendMessage('user', question);
    chatInput.value = '';
    sendBtn.disabled = true;
    chatInput.disabled = true;
    isWaitingForAI = true;

    const typingElement = appendMessage('ai', '', true);

    try {
        const response = await fetch('/Chat/GetAdvice', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ question: question })
        });

        const data = await response.json();
        typingElement.remove();

        if (data.error) {
            appendMessage('ai', `Lỗi AI: ${data.error}`, false);
            isWaitingForAI = false;
            sendBtn.disabled = false;
            chatInput.disabled = false;
            chatInput.focus();
        } else {
            const newAiMessage = appendMessage('ai', '', false);
            typeResponse(newAiMessage, data.advice);
        }
    } catch (error) {
        typingElement.remove();
        appendMessage('ai', 'Lỗi kết nối. Vui lòng thử lại sau.', false);
        isWaitingForAI = false;
        sendBtn.disabled = false;
        chatInput.disabled = false;
        chatInput.focus();
    }
}

// Event listeners
sendBtn.addEventListener('click', sendMessage);
chatInput.addEventListener('keypress', function (e) {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        sendMessage();
    }
});

// Proactive advice
async function getProactiveAdvice() {
    if (isWaitingForAI) return;

    isWaitingForAI = true;
    proactiveAdviceBtn.disabled = true;
    chatInput.disabled = true;
    sendBtn.disabled = true;

    appendMessage('user', 'Hãy cho tôi lời khuyên dinh dưỡng hôm nay!');
    const typingElement = appendMessage('ai', '', true);

    try {
        const response = await fetch('/Chat/GetProactiveAdvice', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({})
        });

        const data = await response.json();
        typingElement.remove();

        if (data.error) {
            appendMessage('ai', `Lỗi AI: ${data.error}`, false);
        } else {
            const newAiMessage = appendMessage('ai', '', false);
            typeResponse(newAiMessage, data.advice);
        }
    } catch (error) {
        typingElement.remove();
        appendMessage('ai', 'Lỗi kết nối. Vui lòng thử lại.', false);
    } finally {
        proactiveAdviceBtn.disabled = false;
        chatInput.disabled = false;
        sendBtn.disabled = false;
        isWaitingForAI = false;
        chatInput.focus();
    }
}

proactiveAdviceBtn.addEventListener('click', getProactiveAdvice);

// Recipe generator
async function requestRecipe() {
    const request = document.getElementById('recipeRequest').value.trim();
    const resultContainer = document.getElementById('recipeResultContainer');
    const generateBtn = document.getElementById('generateRecipeBtn');

    if (request === "") {
        resultContainer.innerHTML = '<div class="alert alert-danger">Vui lòng nhập yêu cầu!</div>';
        return;
    }

    resultContainer.innerHTML = '<div class="alert alert-info"><span class="spinner-border spinner-border-sm me-2"></span>Đang tạo công thức...</div>';
    generateBtn.disabled = true;

    try {
        const response = await fetch('/Chat/GenerateRecipe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ request: request })
        });

        const data = await response.json();
        generateBtn.disabled = false;

        if (data.error) {
            resultContainer.innerHTML = `<div class="alert alert-danger">${data.error}</div>`;
            return;
        }

        renderRecipeResult(data);
    } catch (error) {
        generateBtn.disabled = false;
        resultContainer.innerHTML = '<div class="alert alert-danger">Lỗi kết nối.</div>';
    }
}

// chatbox.js: Sửa hàm renderRecipeResult
function renderRecipeResult(data) {
    const resultContainer = document.getElementById('recipeResultContainer');
    const ingredientsHtml = data.Ingredients.map(item =>
        `<li>${item}</li>`).join('');
    const instructionsHtml = data.Instructions.map(item =>
        `<li>${item}</li>`).join('');
    const recipeDataString = JSON.stringify(data).replace(/"/g, '&quot;');

    // FIX BUG 4.2: Khôi phục toàn bộ giao diện chi tiết công thức và thêm nút lưu vào cuối
    resultContainer.innerHTML = `
    <div class="card">
        <h5>${data.Title}</h5>
        <p class="text-muted">${data.Description}</p>
        <div style="border-top: 1px solid var(--border-color); padding-top: 1rem;
            margin-top: 1rem;">
            <div class="row text-center" style="font-size: 0.875rem;">
                <div class="col-3"><strong>Calo</strong><br>${data.CaloriesTotal.toFixed(0)}</div>
                <div class="col-3"><strong>P</strong><br>${data.ProteinGrams.toFixed(1)}g</div>
                <div class="col-3"><strong>C</strong><br>${data.CarbGrams.toFixed(1)}g</div>
                <div class="col-3"><strong>F</strong><br>${data.FatGrams.toFixed(1)}g</div>
            </div>
        </div>
        <div class="mt-3">
            <h6>Nguyên liệu</h6>
            <ul>${ingredientsHtml}</ul>
            <h6>Hướng dẫn</h6>
            <ol>${instructionsHtml}</ol>
            <div class="alert alert-warning mt-3">
                <strong>Lời khuyên:</strong> ${data.Advice}
            </div>
        </div>
        
        <!-- THÊM CÁC NÚT LƯU Ở CUỐI CARD -->
        <div class="mt-3 d-flex gap-2" style="border-top: 1px solid var(--border-color); padding-top: 15px;">
            <button class="btn btn-sm btn-success"
                onclick='saveAiRecipe(${recipeDataString})'>
                Lưu Thực Đơn
            </button>
            <button class="btn btn-sm btn-primary"
                onclick='saveAiRecipe(${recipeDataString}, true)'>
                Lưu & Chia sẻ Cộng đồng
            </button>
        </div>
    </div>
    `;
    document.getElementById('recipeModeContent').scrollTop = 0;
}

// Hàm mới để lưu recipe từ Chat
async function saveAiRecipe(data, isPublic = false) {
    // Xử lý CookingTime: Nếu là string (VD: "30 phút"), chỉ lấy số đầu tiên
    let cookingTimeValue = 30; // Giá trị mặc định
    if (data.CookingTime) {
        if (typeof data.CookingTime === 'number') {
            cookingTimeValue = data.CookingTime;
        } else if (typeof data.CookingTime === 'string') {
            const match = data.CookingTime.match(/\d+/);
            if (match) {
                cookingTimeValue = parseInt(match);
            }
        }
    }
    const payload = {
        RecipeName: data.Title,
        Description: data.Description,
        Category: "AI Generated", // Hoặc parse từ description
        CookingTime: data.CookingTime || 30,
        Calories: data.CaloriesTotal,
        Protein: data.ProteinGrams,
        Carbs: data.CarbGrams,
        Fat: data.FatGrams,
        Ingredients: data.Ingredients,
        Instructions: data.Instructions,
        IsPublic: isPublic
    };

    // Gọi API Save (Lưu ý: API này cần sửa để nhận JSON nếu không có ảnh, 
    // hoặc dùng FormData như trên nhưng không gửi ảnh)
    // Để đơn giản, ta gọi endpoint SaveRecipeRequest JSON cũ (nhớ revert Controller về [FromBody] nếu không upload ảnh từ chat)
    // HOẶC tạo endpoint riêng cho AI Save

    try {
        // Ta dùng endpoint trung gian ReviewAndSaveRecipe để chuyển hướng sang trang Create cho user review
        const response = await fetch('/Recipe/ReviewAndSaveRecipe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const res = await response.json();
        if (res.success) {
            window.location.href = res.redirectUrl; // Chuyển user sang trang Create đã điền sẵn info
        }
    } catch (e) {
        alert("Lỗi: " + e);
    }
}

// Mode toggles
showChatBtn.addEventListener('click', () => switchChatMode('chat'));
showRecipeBtn.addEventListener('click', () => switchChatMode('recipe'));

// Active nav link
document.addEventListener('DOMContentLoaded', function () {
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.nav-link');

    navLinks.forEach(link => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
});