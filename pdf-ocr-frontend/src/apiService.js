import axios from 'axios';

// const API_URL = process.env.REACT_APP_API_URL || 'https://localhost:7083';

const API_URL = 'http://34.198.178.143';

const apiService = {
  // Document upload
  uploadDocument: async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await axios.post(`${API_URL}/api/documents/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    
    return response.data;
  },
  
  // Get all documents
  getAllDocuments: async () => {
    const response = await axios.get(`${API_URL}/api/documents`);
    return response.data;
  },
  
  // Get document by ID
  getDocumentById: async (id) => {
    const response = await axios.get(`${API_URL}/api/documents/${id}`);
    return response.data;
  },
  
  // Search documents
  searchDocuments: async (query) => {
    const response = await axios.get(`${API_URL}/api/documents/search?query=${encodeURIComponent(query)}`);
    return response.data;
  },
  
  // Get document as JSON
  getDocumentAsJson: async (id) => {
    const response = await axios.get(`${API_URL}/api/documents/${id}/json`, {
      responseType: 'text'
    });
    return response.data;
  },
  
  // Get document as CSV (returns blob)
  getDocumentAsCsv: async (id) => {
    const response = await axios.get(`${API_URL}/api/documents/${id}/csv`, {
      responseType: 'blob'
    });
    return response.data;
  },
  
  // Delete document
  deleteDocument: async (id) => {
    const response = await axios.delete(`${API_URL}/api/documents/${id}`);
    return response.data;
  }
};

export default apiService;