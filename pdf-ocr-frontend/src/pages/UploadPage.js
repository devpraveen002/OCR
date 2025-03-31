import React, { useState } from 'react';
import { Form, Button, Card, Alert, ProgressBar } from 'react-bootstrap';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faFileUpload, faSpinner } from '@fortawesome/free-solid-svg-icons';
import { useNavigate } from 'react-router-dom';
import { API_URL } from '../config';
import axios from 'axios';

const UploadPage = () => {
  const [file, setFile] = useState(null);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState(0);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const navigate = useNavigate();

  const handleFileChange = (e) => {
    const selectedFile = e.target.files[0];
    
    if (selectedFile && selectedFile.type !== 'application/pdf') {
      setError('Please select a PDF file');
      setFile(null);
      return;
    }
    
    setFile(selectedFile);
    setError('');
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!file) {
      setError('Please select a file to upload');
      return;
    }
    
    setUploading(true);
    setProgress(0);
    setError('');
    setSuccess('');
    
    const formData = new FormData();
    formData.append('file', file);
    
    try {
      // const response = await axios.post(
      //   `${process.env.REACT_APP_API_URL}/api/documents/upload`,
      const response = await axios.post(
        `${API_URL}/api/documents/upload`,
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
          onUploadProgress: (progressEvent) => {
            const percentage = Math.round(
              (progressEvent.loaded * 50) / progressEvent.total
            );
            setProgress(percentage); // Only goes to 50% on upload
          },
        }
      );
      
      // Simulate processing time with progress
      let processingProgress = 0;
      const interval = setInterval(() => {
        processingProgress += 5;
        setProgress(50 + Math.min(processingProgress, 50));
        
        if (processingProgress >= 50) {
          clearInterval(interval);
        }
      }, 300);
      
      setSuccess(`Document processed successfully: ${response.data.fileName}`);
      setTimeout(() => {
        navigate(`/documents/${response.data.documentId}`);
      }, 2000);
    } catch (err) {
      setError(
        err.response?.data?.error || 
        'An error occurred while processing the document'
      );
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className="upload-page">
      <Card className="shadow-sm">
        <Card.Header as="h5">Upload PDF Document</Card.Header>
        <Card.Body>
          {error && <Alert variant="danger">{error}</Alert>}
          {success && <Alert variant="success">{success}</Alert>}
          
          <Form onSubmit={handleSubmit}>
            <Form.Group controlId="formFile" className="mb-3">
              <Form.Label>Select a PDF file to process</Form.Label>
              <Form.Control 
                type="file" 
                accept=".pdf" 
                onChange={handleFileChange}
                disabled={uploading}
              />
              <Form.Text className="text-muted">
                Supported format: PDF (Max size: 10MB)
              </Form.Text>
            </Form.Group>
            
            {uploading && (
              <div className="mb-3">
                <ProgressBar 
                  now={progress} 
                  label={`${progress}%`} 
                  animated 
                />
                <div className="text-center mt-2">
                  {progress <= 50 ? 'Uploading...' : 'Processing document...'}
                </div>
              </div>
            )}
            
            <Button 
              variant="primary" 
              type="submit" 
              disabled={!file || uploading}
              className="w-100"
            >
              {uploading ? (
                <>
                  <FontAwesomeIcon icon={faSpinner} spin className="me-2" />
                  Processing...
                </>
              ) : (
                <>
                  <FontAwesomeIcon icon={faFileUpload} className="me-2" />
                  Upload and Process
                </>
              )}
            </Button>
          </Form>
        </Card.Body>
      </Card>

      <Card className="mt-4 shadow-sm">
        <Card.Header as="h5">How It Works</Card.Header>
        <Card.Body>
          <ol>
            <li>Upload a PDF document (invoice, receipt, statement, etc.)</li>
            <li>Our system uses AWS Textract OCR to extract text</li>
            <li>The extracted text is processed to identify key fields like invoice numbers, dates, and amounts</li>
            <li>Results are presented in a structured format that you can view and export</li>
          </ol>
          <p className="text-muted mb-0">
            The system supports various document types including invoices, receipts, statements, and purchase orders.
          </p>
        </Card.Body>
      </Card>
    </div>
  );
};

export default UploadPage;